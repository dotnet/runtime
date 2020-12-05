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

            Console.WriteLine($"const int CulturesCount = {indexes.Count};");

            Console.WriteLine("private static ReadOnlySpan<byte> LocalesNamesIndexes => new byte[CulturesCount * 2]");
            Console.WriteLine("{");
            int max_length = 0;
            foreach (var entry in indexes) {
                Debug.Assert(entry.Item1 < Math.Pow (2,12));
                Debug.Assert(entry.Item2 < Math.Pow (2, 4));

                int index = entry.Item1 << 4 | entry.Item2;
                int high = index >> 8;
                int low = (byte)index;

                Debug.Assert(((high << 4) | (low >> 4)) == entry.Item1);
                Debug.Assert((low & 0xF) == entry.Item1);

                Console.WriteLine($"    {high}, {low},\t// {entry.Item3}");

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
            (byte)'y', (byte)'i', (byte)'-', (byte)'0', (byte)'0', (byte)'1',  // yi, yi-001
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

        private const int CulturesCount = 864;

        // Table which holds index into LocalesNames data and length of the string for each locale
        // Values are binary searched and need to be sorted alphabetically
        //
        // value = index << 4 | length
        // byte0 = value >> 8; byte1 = value & 0xff
        private static ReadOnlySpan<byte> LocalesNamesIndexes => new byte[CulturesCount * 2]
        {
            0, 2,     // aa
            0, 5,     // aa-dj
            0, 85,    // aa-er
            0, 165,   // aa-et
            0, 242,   // af
            0, 245,   // af-na
            1, 69,    // af-za
            1, 147,   // agq
            1, 150,   // agq-cm
            1, 242,   // ak
            1, 245,   // ak-gh
            2, 66,    // am
            2, 69,    // am-et
            2, 146,   // ar
            2, 150,   // ar-001
            2, 245,   // ar-ae
            3, 69,    // ar-bh
            3, 149,   // ar-dj
            3, 229,   // ar-dz
            4, 53,    // ar-eg
            4, 133,   // ar-er
            4, 213,   // ar-il
            5, 37,    // ar-iq
            5, 117,   // ar-jo
            5, 197,   // ar-km
            6, 21,    // ar-kw
            6, 101,   // ar-lb
            6, 181,   // ar-ly
            7, 5,     // ar-ma
            7, 85,    // ar-mr
            7, 165,   // ar-om
            7, 245,   // ar-ps
            8, 69,    // ar-qa
            8, 149,   // ar-sa
            8, 229,   // ar-sd
            9, 53,    // ar-so
            9, 133,   // ar-ss
            9, 213,   // ar-sy
            10, 37,   // ar-td
            10, 117,  // ar-tn
            10, 197,  // ar-ye
            11, 19,   // arn
            11, 22,   // arn-cl
            11, 114,  // as
            11, 117,  // as-in
            11, 195,  // asa
            11, 198,  // asa-tz
            12, 35,   // ast
            12, 38,   // ast-es
            12, 130,  // az
            12, 135,  // az-cyrl
            12, 138,  // az-cyrl-az
            13, 39,   // az-latn
            13, 42,   // az-latn-az
            13, 194,  // ba
            13, 197,  // ba-ru
            14, 19,   // bas
            14, 22,   // bas-cm
            14, 114,  // be
            14, 117,  // be-by
            14, 195,  // bem
            14, 198,  // bem-zm
            15, 35,   // bez
            15, 38,   // bez-tz
            15, 130,  // bg
            15, 133,  // bg-bg
            15, 211,  // bin
            15, 214,  // bin-ng
            16, 50,   // bm
            16, 55,   // bm-latn
            16, 58,   // bm-latn-ml
            16, 210,  // bn
            16, 213,  // bn-bd
            17, 37,   // bn-in
            17, 114,  // bo
            17, 117,  // bo-cn
            17, 197,  // bo-in
            18, 18,   // br
            18, 21,   // br-fr
            18, 99,   // brx
            18, 102,  // brx-in
            18, 194,  // bs
            18, 199,  // bs-cyrl
            18, 202,  // bs-cyrl-ba
            19, 103,  // bs-latn
            19, 106,  // bs-latn-ba
            20, 3,    // byn
            20, 6,    // byn-er
            20, 98,   // ca
            20, 101,  // ca-ad
            20, 181,  // ca-es
            20, 190,  // ca-es-valencia
            21, 149,  // ca-fr
            21, 229,  // ca-it
            22, 50,   // ce
            22, 53,   // ce-ru
            22, 131,  // cgg
            22, 134,  // cgg-ug
            22, 227,  // chr
            22, 232,  // chr-cher
            22, 235,  // chr-cher-us
            23, 146,  // co
            23, 149,  // co-fr
            23, 226,  // cs
            23, 229,  // cs-cz
            24, 50,   // cu
            24, 53,   // cu-ru
            24, 130,  // cy
            24, 133,  // cy-gb
            24, 210,  // da
            24, 213,  // da-dk
            25, 37,   // da-gl
            25, 115,  // dav
            25, 118,  // dav-ke
            25, 210,  // de
            25, 213,  // de-at
            26, 37,   // de-be
            26, 117,  // de-ch
            26, 197,  // de-de
            26, 204,  // de-de_phoneb
            27, 133,  // de-it
            27, 213,  // de-li
            28, 37,   // de-lu
            28, 115,  // dje
            28, 118,  // dje-ne
            28, 211,  // dsb
            28, 214,  // dsb-de
            29, 51,   // dua
            29, 54,   // dua-cm
            29, 146,  // dv
            29, 149,  // dv-mv
            29, 227,  // dyo
            29, 230,  // dyo-sn
            30, 66,   // dz
            30, 69,   // dz-bt
            30, 147,  // ebu
            30, 150,  // ebu-ke
            30, 242,  // ee
            30, 245,  // ee-gh
            31, 69,   // ee-tg
            31, 146,  // el
            31, 149,  // el-cy
            31, 229,  // el-gr
            32, 50,   // en
            32, 54,   // en-001
            32, 150,  // en-029
            32, 246,  // en-150
            33, 85,   // en-ag
            33, 165,  // en-ai
            33, 245,  // en-as
            34, 69,   // en-at
            34, 149,  // en-au
            34, 229,  // en-bb
            35, 53,   // en-be
            35, 133,  // en-bi
            35, 213,  // en-bm
            36, 37,   // en-bs
            36, 117,  // en-bw
            36, 197,  // en-bz
            37, 21,   // en-ca
            37, 101,  // en-cc
            37, 181,  // en-ch
            38, 5,    // en-ck
            38, 85,   // en-cm
            38, 165,  // en-cx
            38, 245,  // en-cy
            39, 69,   // en-de
            39, 149,  // en-dk
            39, 229,  // en-dm
            40, 53,   // en-er
            40, 133,  // en-fi
            40, 213,  // en-fj
            41, 37,   // en-fk
            41, 117,  // en-fm
            41, 197,  // en-gb
            42, 21,   // en-gd
            42, 101,  // en-gg
            42, 181,  // en-gh
            43, 5,    // en-gi
            43, 85,   // en-gm
            43, 165,  // en-gu
            43, 245,  // en-gy
            44, 69,   // en-hk
            44, 149,  // en-id
            44, 229,  // en-ie
            45, 53,   // en-il
            45, 133,  // en-im
            45, 213,  // en-in
            46, 37,   // en-io
            46, 117,  // en-je
            46, 197,  // en-jm
            47, 21,   // en-ke
            47, 101,  // en-ki
            47, 181,  // en-kn
            48, 5,    // en-ky
            48, 85,   // en-lc
            48, 165,  // en-lr
            48, 245,  // en-ls
            49, 69,   // en-mg
            49, 149,  // en-mh
            49, 229,  // en-mo
            50, 53,   // en-mp
            50, 133,  // en-ms
            50, 213,  // en-mt
            51, 37,   // en-mu
            51, 117,  // en-mw
            51, 197,  // en-my
            52, 21,   // en-na
            52, 101,  // en-nf
            52, 181,  // en-ng
            53, 5,    // en-nl
            53, 85,   // en-nr
            53, 165,  // en-nu
            53, 245,  // en-nz
            54, 69,   // en-pg
            54, 149,  // en-ph
            54, 229,  // en-pk
            55, 53,   // en-pn
            55, 133,  // en-pr
            55, 213,  // en-pw
            56, 37,   // en-rw
            56, 117,  // en-sb
            56, 197,  // en-sc
            57, 21,   // en-sd
            57, 101,  // en-se
            57, 181,  // en-sg
            58, 5,    // en-sh
            58, 85,   // en-si
            58, 165,  // en-sl
            58, 245,  // en-ss
            59, 69,   // en-sx
            59, 149,  // en-sz
            59, 229,  // en-tc
            60, 53,   // en-tk
            60, 133,  // en-to
            60, 213,  // en-tt
            61, 37,   // en-tv
            61, 117,  // en-tz
            61, 197,  // en-ug
            62, 21,   // en-um
            62, 101,  // en-us
            62, 181,  // en-vc
            63, 5,    // en-vg
            63, 85,   // en-vi
            63, 165,  // en-vu
            63, 245,  // en-ws
            64, 69,   // en-za
            64, 149,  // en-zm
            64, 229,  // en-zw
            65, 50,   // eo
            65, 54,   // eo-001
            65, 146,  // es
            65, 150,  // es-419
            65, 245,  // es-ar
            66, 69,   // es-bo
            66, 149,  // es-br
            66, 229,  // es-cl
            67, 53,   // es-co
            67, 133,  // es-cr
            67, 213,  // es-cu
            68, 37,   // es-do
            68, 117,  // es-ec
            68, 197,  // es-es
            68, 204,  // es-es_tradnl
            69, 133,  // es-gq
            69, 213,  // es-gt
            70, 37,   // es-hn
            70, 117,  // es-mx
            70, 197,  // es-ni
            71, 21,   // es-pa
            71, 101,  // es-pe
            71, 181,  // es-ph
            72, 5,    // es-pr
            72, 85,   // es-py
            72, 165,  // es-sv
            72, 245,  // es-us
            73, 69,   // es-uy
            73, 149,  // es-ve
            73, 226,  // et
            73, 229,  // et-ee
            74, 50,   // eu
            74, 53,   // eu-es
            74, 131,  // ewo
            74, 134,  // ewo-cm
            74, 226,  // fa
            74, 229,  // fa-ir
            75, 50,   // ff
            75, 53,   // ff-cm
            75, 133,  // ff-gn
            75, 215,  // ff-latn
            75, 218,  // ff-latn-sn
            76, 117,  // ff-mr
            76, 197,  // ff-ng
            77, 18,   // fi
            77, 21,   // fi-fi
            77, 99,   // fil
            77, 102,  // fil-ph
            77, 194,  // fo
            77, 197,  // fo-dk
            78, 21,   // fo-fo
            78, 98,   // fr
            78, 102,  // fr-029
            78, 197,  // fr-be
            79, 21,   // fr-bf
            79, 101,  // fr-bi
            79, 181,  // fr-bj
            80, 5,    // fr-bl
            80, 85,   // fr-ca
            80, 165,  // fr-cd
            80, 245,  // fr-cf
            81, 69,   // fr-cg
            81, 149,  // fr-ch
            81, 229,  // fr-ci
            82, 53,   // fr-cm
            82, 133,  // fr-dj
            82, 213,  // fr-dz
            83, 37,   // fr-fr
            83, 117,  // fr-ga
            83, 197,  // fr-gf
            84, 21,   // fr-gn
            84, 101,  // fr-gp
            84, 181,  // fr-gq
            85, 5,    // fr-ht
            85, 85,   // fr-km
            85, 165,  // fr-lu
            85, 245,  // fr-ma
            86, 69,   // fr-mc
            86, 149,  // fr-mf
            86, 229,  // fr-mg
            87, 53,   // fr-ml
            87, 133,  // fr-mq
            87, 213,  // fr-mr
            88, 37,   // fr-mu
            88, 117,  // fr-nc
            88, 197,  // fr-ne
            89, 21,   // fr-pf
            89, 101,  // fr-pm
            89, 181,  // fr-re
            90, 5,    // fr-rw
            90, 85,   // fr-sc
            90, 165,  // fr-sn
            90, 245,  // fr-sy
            91, 69,   // fr-td
            91, 149,  // fr-tg
            91, 229,  // fr-tn
            92, 53,   // fr-vu
            92, 133,  // fr-wf
            92, 213,  // fr-yt
            93, 35,   // fur
            93, 38,   // fur-it
            93, 130,  // fy
            93, 133,  // fy-nl
            93, 210,  // ga
            93, 213,  // ga-ie
            94, 34,   // gd
            94, 37,   // gd-gb
            94, 114,  // gl
            94, 117,  // gl-es
            94, 194,  // gn
            94, 197,  // gn-py
            95, 19,   // gsw
            95, 22,   // gsw-ch
            95, 118,  // gsw-fr
            95, 214,  // gsw-li
            96, 50,   // gu
            96, 53,   // gu-in
            96, 131,  // guz
            96, 134,  // guz-ke
            96, 226,  // gv
            96, 229,  // gv-im
            97, 50,   // ha
            97, 55,   // ha-latn
            97, 58,   // ha-latn-gh
            97, 218,  // ha-latn-ne
            98, 122,  // ha-latn-ng
            99, 19,   // haw
            99, 22,   // haw-us
            99, 114,  // he
            99, 117,  // he-il
            99, 194,  // hi
            99, 197,  // hi-in
            100, 18,  // hr
            100, 21,  // hr-ba
            100, 101, // hr-hr
            100, 179, // hsb
            100, 182, // hsb-de
            101, 18,  // hu
            101, 21,  // hu-hu
            101, 28,  // hu-hu_technl
            101, 210, // hy
            101, 213, // hy-am
            102, 34,  // ia
            102, 38,  // ia-001
            102, 133, // ia-fr
            102, 211, // ibb
            102, 214, // ibb-ng
            103, 50,  // id
            103, 53,  // id-id
            103, 130, // ig
            103, 133, // ig-ng
            103, 210, // ii
            103, 213, // ii-cn
            104, 34,  // is
            104, 37,  // is-is
            104, 114, // it
            104, 117, // it-ch
            104, 197, // it-it
            105, 21,  // it-sm
            105, 98,  // iu
            105, 103, // iu-cans
            105, 106, // iu-cans-ca
            106, 7,   // iu-latn
            106, 10,  // iu-latn-ca
            106, 162, // ja
            106, 165, // ja-jp
            106, 172, // ja-jp_radstr
            107, 99,  // jgo
            107, 102, // jgo-cm
            107, 195, // jmc
            107, 198, // jmc-tz
            108, 34,  // jv
            108, 39,  // jv-java
            108, 42,  // jv-java-id
            108, 199, // jv-latn
            108, 202, // jv-latn-id
            109, 98,  // ka
            109, 101, // ka-ge
            109, 108, // ka-ge_modern
            110, 35,  // kab
            110, 38,  // kab-dz
            110, 131, // kam
            110, 134, // kam-ke
            110, 227, // kde
            110, 230, // kde-tz
            111, 67,  // kea
            111, 70,  // kea-cv
            111, 163, // khq
            111, 166, // khq-ml
            112, 2,   // ki
            112, 5,   // ki-ke
            112, 82,  // kk
            112, 85,  // kk-kz
            112, 163, // kkj
            112, 166, // kkj-cm
            113, 2,   // kl
            113, 5,   // kl-gl
            113, 83,  // kln
            113, 86,  // kln-ke
            113, 178, // km
            113, 181, // km-kh
            114, 2,   // kn
            114, 5,   // kn-in
            114, 82,  // ko
            114, 85,  // ko-kp
            114, 165, // ko-kr
            114, 243, // kok
            114, 246, // kok-in
            115, 82,  // kr
            115, 85,  // kr-ng
            115, 162, // ks
            115, 167, // ks-arab
            115, 170, // ks-arab-in
            116, 71,  // ks-deva
            116, 74,  // ks-deva-in
            116, 227, // ksb
            116, 230, // ksb-tz
            117, 67,  // ksf
            117, 70,  // ksf-cm
            117, 163, // ksh
            117, 166, // ksh-de
            118, 2,   // ku
            118, 7,   // ku-arab
            118, 10,  // ku-arab-iq
            118, 170, // ku-arab-ir
            119, 66,  // kw
            119, 69,  // kw-gb
            119, 146, // ky
            119, 149, // ky-kg
            119, 226, // la
            119, 230, // la-001
            120, 67,  // lag
            120, 70,  // lag-tz
            120, 162, // lb
            120, 165, // lb-lu
            120, 242, // lg
            120, 245, // lg-ug
            121, 67,  // lkt
            121, 70,  // lkt-us
            121, 162, // ln
            121, 165, // ln-ao
            121, 245, // ln-cd
            122, 69,  // ln-cf
            122, 149, // ln-cg
            122, 226, // lo
            122, 229, // lo-la
            123, 51,  // lrc
            123, 54,  // lrc-iq
            123, 150, // lrc-ir
            123, 242, // lt
            123, 245, // lt-lt
            124, 66,  // lu
            124, 69,  // lu-cd
            124, 147, // luo
            124, 150, // luo-ke
            124, 243, // luy
            124, 246, // luy-ke
            125, 82,  // lv
            125, 85,  // lv-lv
            125, 163, // mas
            125, 166, // mas-ke
            126, 6,   // mas-tz
            126, 99,  // mer
            126, 102, // mer-ke
            126, 195, // mfe
            126, 198, // mfe-mu
            127, 34,  // mg
            127, 37,  // mg-mg
            127, 115, // mgh
            127, 118, // mgh-mz
            127, 211, // mgo
            127, 214, // mgo-cm
            128, 50,  // mi
            128, 53,  // mi-nz
            128, 130, // mk
            128, 133, // mk-mk
            128, 210, // ml
            128, 213, // ml-in
            129, 34,  // mn
            129, 39,  // mn-cyrl
            129, 149, // mn-mn
            129, 231, // mn-mong
            129, 234, // mn-mong-cn
            130, 138, // mn-mong-mn
            131, 35,  // mni
            131, 38,  // mni-in
            131, 131, // moh
            131, 134, // moh-ca
            131, 226, // mr
            131, 229, // mr-in
            132, 50,  // ms
            132, 53,  // ms-bn
            132, 133, // ms-my
            132, 213, // ms-sg
            133, 34,  // mt
            133, 37,  // mt-mt
            133, 115, // mua
            133, 118, // mua-cm
            133, 210, // my
            133, 213, // my-mm
            134, 35,  // mzn
            134, 38,  // mzn-ir
            134, 131, // naq
            134, 134, // naq-na
            134, 226, // nb
            134, 229, // nb-no
            135, 53,  // nb-sj
            135, 130, // nd
            135, 133, // nd-zw
            135, 211, // nds
            135, 214, // nds-de
            136, 54,  // nds-nl
            136, 146, // ne
            136, 149, // ne-in
            136, 229, // ne-np
            137, 50,  // nl
            137, 53,  // nl-aw
            137, 133, // nl-be
            137, 213, // nl-bq
            138, 37,  // nl-cw
            138, 117, // nl-nl
            138, 197, // nl-sr
            139, 21,  // nl-sx
            139, 99,  // nmg
            139, 102, // nmg-cm
            139, 194, // nn
            139, 197, // nn-no
            140, 19,  // nnh
            140, 22,  // nnh-cm
            140, 114, // no
            140, 147, // nqo
            140, 150, // nqo-gn
            140, 242, // nr
            140, 245, // nr-za
            141, 67,  // nso
            141, 70,  // nso-za
            141, 163, // nus
            141, 166, // nus-ss
            142, 3,   // nyn
            142, 6,   // nyn-ug
            142, 98,  // oc
            142, 101, // oc-fr
            142, 178, // om
            142, 181, // om-et
            143, 5,   // om-ke
            143, 82,  // or
            143, 85,  // or-in
            143, 162, // os
            143, 165, // os-ge
            143, 245, // os-ru
            144, 66,  // pa
            144, 71,  // pa-arab
            144, 74,  // pa-arab-pk
            144, 229, // pa-in
            145, 51,  // pap
            145, 55,  // pap-029
            145, 162, // pl
            145, 165, // pl-pl
            145, 243, // prg
            145, 247, // prg-001
            146, 99,  // prs
            146, 102, // prs-af
            146, 194, // ps
            146, 197, // ps-af
            147, 18,  // pt
            147, 21,  // pt-ao
            147, 101, // pt-br
            147, 181, // pt-ch
            148, 5,   // pt-cv
            148, 85,  // pt-gq
            148, 165, // pt-gw
            148, 245, // pt-lu
            149, 69,  // pt-mo
            149, 149, // pt-mz
            149, 229, // pt-pt
            150, 53,  // pt-st
            150, 133, // pt-tl
            150, 221, // qps-latn-x-sh
            151, 168, // qps-ploc
            151, 169, // qps-ploca
            152, 57,  // qps-plocm
            152, 195, // quc
            152, 200, // quc-latn
            152, 203, // quc-latn-gt
            153, 115, // quz
            153, 118, // quz-bo
            153, 214, // quz-ec
            154, 54,  // quz-pe
            154, 146, // rm
            154, 149, // rm-ch
            154, 226, // rn
            154, 229, // rn-bi
            155, 50,  // ro
            155, 53,  // ro-md
            155, 133, // ro-ro
            155, 211, // rof
            155, 214, // rof-tz
            156, 50,  // ru
            156, 53,  // ru-by
            156, 133, // ru-kg
            156, 213, // ru-kz
            157, 37,  // ru-md
            157, 117, // ru-ru
            157, 197, // ru-ua
            158, 18,  // rw
            158, 21,  // rw-rw
            158, 99,  // rwk
            158, 102, // rwk-tz
            158, 194, // sa
            158, 197, // sa-in
            159, 19,  // sah
            159, 22,  // sah-ru
            159, 115, // saq
            159, 118, // saq-ke
            159, 211, // sbp
            159, 214, // sbp-tz
            160, 50,  // sd
            160, 55,  // sd-arab
            160, 58,  // sd-arab-pk
            160, 215, // sd-deva
            160, 218, // sd-deva-in
            161, 114, // se
            161, 117, // se-fi
            161, 197, // se-no
            162, 21,  // se-se
            162, 99,  // seh
            162, 102, // seh-mz
            162, 195, // ses
            162, 198, // ses-ml
            163, 34,  // sg
            163, 37,  // sg-cf
            163, 115, // shi
            163, 120, // shi-latn
            163, 123, // shi-latn-ma
            164, 40,  // shi-tfng
            164, 43,  // shi-tfng-ma
            164, 210, // si
            164, 213, // si-lk
            165, 34,  // sk
            165, 37,  // sk-sk
            165, 114, // sl
            165, 117, // sl-si
            165, 195, // sma
            165, 198, // sma-no
            166, 38,  // sma-se
            166, 131, // smj
            166, 134, // smj-no
            166, 230, // smj-se
            167, 67,  // smn
            167, 70,  // smn-fi
            167, 163, // sms
            167, 166, // sms-fi
            168, 2,   // sn
            168, 7,   // sn-latn
            168, 10,  // sn-latn-zw
            168, 162, // so
            168, 165, // so-dj
            168, 245, // so-et
            169, 69,  // so-ke
            169, 149, // so-so
            169, 226, // sq
            169, 229, // sq-al
            170, 53,  // sq-mk
            170, 133, // sq-xk
            170, 210, // sr
            170, 215, // sr-cyrl
            170, 218, // sr-cyrl-ba
            171, 122, // sr-cyrl-cs
            172, 26,  // sr-cyrl-me
            172, 186, // sr-cyrl-rs
            173, 90,  // sr-cyrl-xk
            173, 247, // sr-latn
            173, 250, // sr-latn-ba
            174, 154, // sr-latn-cs
            175, 58,  // sr-latn-me
            175, 218, // sr-latn-rs
            176, 122, // sr-latn-xk
            177, 18,  // ss
            177, 21,  // ss-sz
            177, 101, // ss-za
            177, 179, // ssy
            177, 182, // ssy-er
            178, 18,  // st
            178, 21,  // st-ls
            178, 101, // st-za
            178, 178, // sv
            178, 181, // sv-ax
            179, 5,   // sv-fi
            179, 85,  // sv-se
            179, 162, // sw
            179, 165, // sw-cd
            179, 245, // sw-ke
            180, 69,  // sw-tz
            180, 149, // sw-ug
            180, 227, // swc
            180, 230, // swc-cd
            181, 67,  // syr
            181, 70,  // syr-sy
            181, 162, // ta
            181, 165, // ta-in
            181, 245, // ta-lk
            182, 69,  // ta-my
            182, 149, // ta-sg
            182, 226, // te
            182, 229, // te-in
            183, 51,  // teo
            183, 54,  // teo-ke
            183, 150, // teo-ug
            183, 242, // tg
            183, 247, // tg-cyrl
            183, 250, // tg-cyrl-tj
            184, 146, // th
            184, 149, // th-th
            184, 226, // ti
            184, 229, // ti-er
            185, 53,  // ti-et
            185, 131, // tig
            185, 134, // tig-er
            185, 226, // tk
            185, 229, // tk-tm
            186, 50,  // tn
            186, 53,  // tn-bw
            186, 133, // tn-za
            186, 210, // to
            186, 213, // to-to
            187, 34,  // tr
            187, 37,  // tr-cy
            187, 117, // tr-tr
            187, 194, // ts
            187, 197, // ts-za
            188, 18,  // tt
            188, 21,  // tt-ru
            188, 99,  // twq
            188, 102, // twq-ne
            188, 195, // tzm
            188, 200, // tzm-arab
            188, 203, // tzm-arab-ma
            189, 120, // tzm-latn
            189, 123, // tzm-latn-dz
            190, 43,  // tzm-latn-ma
            190, 216, // tzm-tfng
            190, 219, // tzm-tfng-ma
            191, 130, // ug
            191, 133, // ug-cn
            191, 210, // uk
            191, 213, // uk-ua
            192, 34,  // ur
            192, 37,  // ur-in
            192, 117, // ur-pk
            192, 194, // uz
            192, 199, // uz-arab
            192, 202, // uz-arab-af
            193, 103, // uz-cyrl
            193, 106, // uz-cyrl-uz
            194, 7,   // uz-latn
            194, 10,  // uz-latn-uz
            194, 163, // vai
            194, 168, // vai-latn
            194, 171, // vai-latn-lr
            195, 88,  // vai-vaii
            195, 91,  // vai-vaii-lr
            196, 2,   // ve
            196, 5,   // ve-za
            196, 82,  // vi
            196, 85,  // vi-vn
            196, 162, // vo
            196, 166, // vo-001
            197, 3,   // vun
            197, 6,   // vun-tz
            197, 99,  // wae
            197, 102, // wae-ch
            197, 195, // wal
            197, 198, // wal-et
            198, 34,  // wo
            198, 37,  // wo-sn
            198, 123, // x-iv_mathan
            199, 34,  // xh
            199, 37,  // xh-za
            199, 115, // xog
            199, 118, // xog-ug
            199, 211, // yav
            199, 214, // yav-cm
            200, 50,  // yi
            200, 54,  // yi-001
            200, 146, // yo
            200, 149, // yo-bj
            200, 229, // yo-ng
            201, 51,  // yue
            201, 54,  // yue-hk
            201, 147, // zgh
            201, 152, // zgh-tfng
            201, 155, // zgh-tfng-ma
            202, 66,  // zh
            202, 70,  // zh-chs
            202, 166, // zh-cht
            203, 5,   // zh-cn
            203, 12,  // zh-cn_phoneb
            203, 204, // zh-cn_stroke
            204, 135, // zh-hans
            204, 138, // zh-hans-hk
            205, 42,  // zh-hans-mo
            205, 199, // zh-hant
            206, 53,  // zh-hk
            206, 60,  // zh-hk_radstr
            206, 245, // zh-mo
            206, 252, // zh-mo_radstr
            207, 188, // zh-mo_stroke
            208, 117, // zh-sg
            208, 124, // zh-sg_phoneb
            209, 60,  // zh-sg_stroke
            209, 245, // zh-tw
            209, 252, // zh-tw_pronun
            210, 188, // zh-tw_radstr
            211, 114, // zu
            211, 117, // zu-za
        };

        private const int LocaleLongestName = 14;
        private const int LcidCount = 448;

        private static ReadOnlySpan<byte> LcidToCultureNameIndices => new byte[LcidCount * 4]
        {
            0x00, 0x01, 0x02, 0x92,  // ar
            0x00, 0x02, 0x0f, 0x82,  // bg
            0x00, 0x03, 0x14, 0x62,  // ca
            0x00, 0x04, 0xca, 0x46,  // zh-chs
            0x00, 0x05, 0x17, 0xe2,  // cs
            0x00, 0x06, 0x18, 0xd2,  // da
            0x00, 0x07, 0x19, 0xd2,  // de
            0x00, 0x08, 0x1f, 0x92,  // el
            0x00, 0x09, 0x20, 0x32,  // en
            0x00, 0x0a, 0x41, 0x92,  // es
            0x00, 0x0b, 0x4d, 0x12,  // fi
            0x00, 0x0c, 0x4e, 0x62,  // fr
            0x00, 0x0d, 0x63, 0x72,  // he
            0x00, 0x0e, 0x65, 0x12,  // hu
            0x00, 0x0f, 0x68, 0x22,  // is
            0x00, 0x10, 0x68, 0x72,  // it
            0x00, 0x11, 0x6a, 0xa2,  // ja
            0x00, 0x12, 0x72, 0x52,  // ko
            0x00, 0x13, 0x89, 0x32,  // nl
            0x00, 0x14, 0x8c, 0x72,  // no
            0x00, 0x15, 0x91, 0xa2,  // pl
            0x00, 0x16, 0x93, 0x12,  // pt
            0x00, 0x17, 0x9a, 0x92,  // rm
            0x00, 0x18, 0x9b, 0x32,  // ro
            0x00, 0x19, 0x9c, 0x32,  // ru
            0x00, 0x1a, 0x64, 0x12,  // hr
            0x00, 0x1b, 0xa5, 0x22,  // sk
            0x00, 0x1c, 0xa9, 0xe2,  // sq
            0x00, 0x1d, 0xb2, 0xb2,  // sv
            0x00, 0x1e, 0xb8, 0x92,  // th
            0x00, 0x1f, 0xbb, 0x22,  // tr
            0x00, 0x20, 0xc0, 0x22,  // ur
            0x00, 0x21, 0x67, 0x32,  // id
            0x00, 0x22, 0xbf, 0xd2,  // uk
            0x00, 0x23, 0x0e, 0x72,  // be
            0x00, 0x24, 0xa5, 0x72,  // sl
            0x00, 0x25, 0x49, 0xe2,  // et
            0x00, 0x26, 0x7d, 0x52,  // lv
            0x00, 0x27, 0x7b, 0xf2,  // lt
            0x00, 0x28, 0xb7, 0xf2,  // tg
            0x00, 0x29, 0x4a, 0xe2,  // fa
            0x00, 0x2a, 0xc4, 0x52,  // vi
            0x00, 0x2b, 0x65, 0xd2,  // hy
            0x00, 0x2c, 0x0c, 0x82,  // az
            0x00, 0x2d, 0x4a, 0x32,  // eu
            0x00, 0x2e, 0x64, 0xb3,  // hsb
            0x00, 0x2f, 0x80, 0x82,  // mk
            0x00, 0x30, 0xb2, 0x12,  // st
            0x00, 0x31, 0xbb, 0xc2,  // ts
            0x00, 0x32, 0xba, 0x32,  // tn
            0x00, 0x33, 0xc4, 0x02,  // ve
            0x00, 0x34, 0xc7, 0x22,  // xh
            0x00, 0x35, 0xd3, 0x72,  // zu
            0x00, 0x36, 0x00, 0xf2,  // af
            0x00, 0x37, 0x6d, 0x62,  // ka
            0x00, 0x38, 0x4d, 0xc2,  // fo
            0x00, 0x39, 0x63, 0xc2,  // hi
            0x00, 0x3a, 0x85, 0x22,  // mt
            0x00, 0x3b, 0xa1, 0x72,  // se
            0x00, 0x3c, 0x5d, 0xd2,  // ga
            0x00, 0x3d, 0xc8, 0x32,  // yi
            0x00, 0x3e, 0x84, 0x32,  // ms
            0x00, 0x3f, 0x70, 0x52,  // kk
            0x00, 0x40, 0x77, 0x92,  // ky
            0x00, 0x41, 0xb3, 0xa2,  // sw
            0x00, 0x42, 0xb9, 0xe2,  // tk
            0x00, 0x43, 0xc0, 0xc2,  // uz
            0x00, 0x44, 0xbc, 0x12,  // tt
            0x00, 0x45, 0x10, 0xd2,  // bn
            0x00, 0x46, 0x90, 0x42,  // pa
            0x00, 0x47, 0x60, 0x32,  // gu
            0x00, 0x48, 0x8f, 0x52,  // or
            0x00, 0x49, 0xb5, 0xa2,  // ta
            0x00, 0x4a, 0xb6, 0xe2,  // te
            0x00, 0x4b, 0x72, 0x02,  // kn
            0x00, 0x4c, 0x80, 0xd2,  // ml
            0x00, 0x4d, 0x0b, 0x72,  // as
            0x00, 0x4e, 0x83, 0xe2,  // mr
            0x00, 0x4f, 0x9e, 0xc2,  // sa
            0x00, 0x50, 0x81, 0x22,  // mn
            0x00, 0x51, 0x11, 0x72,  // bo
            0x00, 0x52, 0x18, 0x82,  // cy
            0x00, 0x53, 0x71, 0xb2,  // km
            0x00, 0x54, 0x7a, 0xe2,  // lo
            0x00, 0x55, 0x85, 0xd2,  // my
            0x00, 0x56, 0x5e, 0x72,  // gl
            0x00, 0x57, 0x72, 0xf3,  // kok
            0x00, 0x58, 0x83, 0x23,  // mni
            0x00, 0x59, 0xa0, 0x32,  // sd
            0x00, 0x5a, 0xb5, 0x43,  // syr
            0x00, 0x5b, 0xa4, 0xd2,  // si
            0x00, 0x5c, 0x16, 0xe3,  // chr
            0x00, 0x5d, 0x69, 0x62,  // iu
            0x00, 0x5e, 0x02, 0x42,  // am
            0x00, 0x5f, 0xbc, 0xc3,  // tzm
            0x00, 0x60, 0x73, 0xa2,  // ks
            0x00, 0x61, 0x88, 0x92,  // ne
            0x00, 0x62, 0x5d, 0x82,  // fy
            0x00, 0x63, 0x92, 0xc2,  // ps
            0x00, 0x64, 0x4d, 0x63,  // fil
            0x00, 0x65, 0x1d, 0x92,  // dv
            0x00, 0x66, 0x0f, 0xd3,  // bin
            0x00, 0x67, 0x4b, 0x32,  // ff
            0x00, 0x68, 0x61, 0x32,  // ha
            0x00, 0x69, 0x66, 0xd3,  // ibb
            0x00, 0x6a, 0xc8, 0x92,  // yo
            0x00, 0x6b, 0x99, 0x73,  // quz
            0x00, 0x6c, 0x8d, 0x43,  // nso
            0x00, 0x6d, 0x0d, 0xc2,  // ba
            0x00, 0x6e, 0x78, 0xa2,  // lb
            0x00, 0x6f, 0x71, 0x02,  // kl
            0x00, 0x70, 0x67, 0x82,  // ig
            0x00, 0x71, 0x73, 0x52,  // kr
            0x00, 0x72, 0x8e, 0xb2,  // om
            0x00, 0x73, 0xb8, 0xe2,  // ti
            0x00, 0x74, 0x5e, 0xc2,  // gn
            0x00, 0x75, 0x63, 0x13,  // haw
            0x00, 0x76, 0x77, 0xe2,  // la
            0x00, 0x77, 0xa8, 0xa2,  // so
            0x00, 0x78, 0x67, 0xd2,  // ii
            0x00, 0x79, 0x91, 0x33,  // pap
            0x00, 0x7a, 0x0b, 0x13,  // arn
            0x00, 0x7c, 0x83, 0x83,  // moh
            0x00, 0x7e, 0x12, 0x12,  // br
            0x00, 0x80, 0xbf, 0x82,  // ug
            0x00, 0x81, 0x80, 0x32,  // mi
            0x00, 0x82, 0x8e, 0x62,  // oc
            0x00, 0x83, 0x17, 0x92,  // co
            0x00, 0x84, 0x5f, 0x13,  // gsw
            0x00, 0x85, 0x9f, 0x13,  // sah
            0x00, 0x86, 0x98, 0xc3,  // quc
            0x00, 0x87, 0x9e, 0x12,  // rw
            0x00, 0x88, 0xc6, 0x22,  // wo
            0x00, 0x8c, 0x92, 0x63,  // prs
            0x00, 0x91, 0x5e, 0x22,  // gd
            0x00, 0x92, 0x76, 0x02,  // ku
            0x04, 0x01, 0x08, 0x95,  // ar-sa
            0x04, 0x02, 0x0f, 0x85,  // bg-bg
            0x04, 0x03, 0x14, 0xb5,  // ca-es
            0x04, 0x04, 0xd1, 0xf5,  // zh-tw
            0x04, 0x05, 0x17, 0xe5,  // cs-cz
            0x04, 0x06, 0x18, 0xd5,  // da-dk
            0x04, 0x07, 0x1a, 0xc5,  // de-de
            0x04, 0x08, 0x1f, 0xe5,  // el-gr
            0x04, 0x09, 0x3e, 0x65,  // en-us
            0x04, 0x0a, 0x44, 0xcc,  // es-es_tradnl
            0x04, 0x0b, 0x4d, 0x15,  // fi-fi
            0x04, 0x0c, 0x53, 0x25,  // fr-fr
            0x04, 0x0d, 0x63, 0x75,  // he-il
            0x04, 0x0e, 0x65, 0x15,  // hu-hu
            0x04, 0x0f, 0x68, 0x25,  // is-is
            0x04, 0x10, 0x68, 0xc5,  // it-it
            0x04, 0x11, 0x6a, 0xa5,  // ja-jp
            0x04, 0x12, 0x72, 0xa5,  // ko-kr
            0x04, 0x13, 0x8a, 0x75,  // nl-nl
            0x04, 0x14, 0x86, 0xe5,  // nb-no
            0x04, 0x15, 0x91, 0xa5,  // pl-pl
            0x04, 0x16, 0x93, 0x65,  // pt-br
            0x04, 0x17, 0x9a, 0x95,  // rm-ch
            0x04, 0x18, 0x9b, 0x85,  // ro-ro
            0x04, 0x19, 0x9d, 0x75,  // ru-ru
            0x04, 0x1a, 0x64, 0x65,  // hr-hr
            0x04, 0x1b, 0xa5, 0x25,  // sk-sk
            0x04, 0x1c, 0xa9, 0xe5,  // sq-al
            0x04, 0x1d, 0xb3, 0x55,  // sv-se
            0x04, 0x1e, 0xb8, 0x95,  // th-th
            0x04, 0x1f, 0xbb, 0x75,  // tr-tr
            0x04, 0x20, 0xc0, 0x75,  // ur-pk
            0x04, 0x21, 0x67, 0x35,  // id-id
            0x04, 0x22, 0xbf, 0xd5,  // uk-ua
            0x04, 0x23, 0x0e, 0x75,  // be-by
            0x04, 0x24, 0xa5, 0x75,  // sl-si
            0x04, 0x25, 0x49, 0xe5,  // et-ee
            0x04, 0x26, 0x7d, 0x55,  // lv-lv
            0x04, 0x27, 0x7b, 0xf5,  // lt-lt
            0x04, 0x28, 0xb7, 0xfa,  // tg-cyrl-tj
            0x04, 0x29, 0x4a, 0xe5,  // fa-ir
            0x04, 0x2a, 0xc4, 0x55,  // vi-vn
            0x04, 0x2b, 0x65, 0xd5,  // hy-am
            0x04, 0x2c, 0x0d, 0x2a,  // az-latn-az
            0x04, 0x2d, 0x4a, 0x35,  // eu-es
            0x04, 0x2e, 0x64, 0xb6,  // hsb-de
            0x04, 0x2f, 0x80, 0x85,  // mk-mk
            0x04, 0x30, 0xb2, 0x65,  // st-za
            0x04, 0x31, 0xbb, 0xc5,  // ts-za
            0x04, 0x32, 0xba, 0x85,  // tn-za
            0x04, 0x33, 0xc4, 0x05,  // ve-za
            0x04, 0x34, 0xc7, 0x25,  // xh-za
            0x04, 0x35, 0xd3, 0x75,  // zu-za
            0x04, 0x36, 0x01, 0x45,  // af-za
            0x04, 0x37, 0x6d, 0x65,  // ka-ge
            0x04, 0x38, 0x4e, 0x15,  // fo-fo
            0x04, 0x39, 0x63, 0xc5,  // hi-in
            0x04, 0x3a, 0x85, 0x25,  // mt-mt
            0x04, 0x3b, 0xa1, 0xc5,  // se-no
            0x04, 0x3d, 0xc8, 0x36,  // yi-001
            0x04, 0x3e, 0x84, 0x85,  // ms-my
            0x04, 0x3f, 0x70, 0x55,  // kk-kz
            0x04, 0x40, 0x77, 0x95,  // ky-kg
            0x04, 0x41, 0xb3, 0xf5,  // sw-ke
            0x04, 0x42, 0xb9, 0xe5,  // tk-tm
            0x04, 0x43, 0xc2, 0x0a,  // uz-latn-uz
            0x04, 0x44, 0xbc, 0x15,  // tt-ru
            0x04, 0x45, 0x11, 0x25,  // bn-in
            0x04, 0x46, 0x90, 0xe5,  // pa-in
            0x04, 0x47, 0x60, 0x35,  // gu-in
            0x04, 0x48, 0x8f, 0x55,  // or-in
            0x04, 0x49, 0xb5, 0xa5,  // ta-in
            0x04, 0x4a, 0xb6, 0xe5,  // te-in
            0x04, 0x4b, 0x72, 0x05,  // kn-in
            0x04, 0x4c, 0x80, 0xd5,  // ml-in
            0x04, 0x4d, 0x0b, 0x75,  // as-in
            0x04, 0x4e, 0x83, 0xe5,  // mr-in
            0x04, 0x4f, 0x9e, 0xc5,  // sa-in
            0x04, 0x50, 0x81, 0x95,  // mn-mn
            0x04, 0x51, 0x11, 0x75,  // bo-cn
            0x04, 0x52, 0x18, 0x85,  // cy-gb
            0x04, 0x53, 0x71, 0xb5,  // km-kh
            0x04, 0x54, 0x7a, 0xe5,  // lo-la
            0x04, 0x55, 0x85, 0xd5,  // my-mm
            0x04, 0x56, 0x5e, 0x75,  // gl-es
            0x04, 0x57, 0x72, 0xf6,  // kok-in
            0x04, 0x58, 0x83, 0x26,  // mni-in
            0x04, 0x59, 0xa0, 0xda,  // sd-deva-in
            0x04, 0x5a, 0xb5, 0x46,  // syr-sy
            0x04, 0x5b, 0xa4, 0xd5,  // si-lk
            0x04, 0x5c, 0x16, 0xeb,  // chr-cher-us
            0x04, 0x5d, 0x69, 0x6a,  // iu-cans-ca
            0x04, 0x5e, 0x02, 0x45,  // am-et
            0x04, 0x5f, 0xbc, 0xcb,  // tzm-arab-ma
            0x04, 0x60, 0x73, 0xa7,  // ks-arab
            0x04, 0x61, 0x88, 0xe5,  // ne-np
            0x04, 0x62, 0x5d, 0x85,  // fy-nl
            0x04, 0x63, 0x92, 0xc5,  // ps-af
            0x04, 0x64, 0x4d, 0x66,  // fil-ph
            0x04, 0x65, 0x1d, 0x95,  // dv-mv
            0x04, 0x66, 0x0f, 0xd6,  // bin-ng
            0x04, 0x67, 0x4c, 0xc5,  // ff-ng
            0x04, 0x68, 0x62, 0x7a,  // ha-latn-ng
            0x04, 0x69, 0x66, 0xd6,  // ibb-ng
            0x04, 0x6a, 0xc8, 0xe5,  // yo-ng
            0x04, 0x6b, 0x99, 0x76,  // quz-bo
            0x04, 0x6c, 0x8d, 0x46,  // nso-za
            0x04, 0x6d, 0x0d, 0xc5,  // ba-ru
            0x04, 0x6e, 0x78, 0xa5,  // lb-lu
            0x04, 0x6f, 0x71, 0x05,  // kl-gl
            0x04, 0x70, 0x67, 0x85,  // ig-ng
            0x04, 0x71, 0x73, 0x55,  // kr-ng
            0x04, 0x72, 0x8e, 0xb5,  // om-et
            0x04, 0x73, 0xb9, 0x35,  // ti-et
            0x04, 0x74, 0x5e, 0xc5,  // gn-py
            0x04, 0x75, 0x63, 0x16,  // haw-us
            0x04, 0x76, 0x77, 0xe6,  // la-001
            0x04, 0x77, 0xa9, 0x95,  // so-so
            0x04, 0x78, 0x67, 0xd5,  // ii-cn
            0x04, 0x79, 0x91, 0x37,  // pap-029
            0x04, 0x7a, 0x0b, 0x16,  // arn-cl
            0x04, 0x7c, 0x83, 0x86,  // moh-ca
            0x04, 0x7e, 0x12, 0x15,  // br-fr
            0x04, 0x80, 0xbf, 0x85,  // ug-cn
            0x04, 0x81, 0x80, 0x35,  // mi-nz
            0x04, 0x82, 0x8e, 0x65,  // oc-fr
            0x04, 0x83, 0x17, 0x95,  // co-fr
            0x04, 0x84, 0x5f, 0x76,  // gsw-fr
            0x04, 0x85, 0x9f, 0x16,  // sah-ru
            0x04, 0x86, 0x98, 0xcb,  // quc-latn-gt
            0x04, 0x87, 0x9e, 0x15,  // rw-rw
            0x04, 0x88, 0xc6, 0x25,  // wo-sn
            0x04, 0x8c, 0x92, 0x66,  // prs-af
            0x04, 0x91, 0x5e, 0x25,  // gd-gb
            0x04, 0x92, 0x76, 0x0a,  // ku-arab-iq
            0x05, 0x01, 0x97, 0xa8,  // qps-ploc
            0x05, 0xfe, 0x97, 0xa9,  // qps-ploca
            0x08, 0x01, 0x05, 0x25,  // ar-iq
            0x08, 0x03, 0x14, 0xbe,  // ca-es-valencia
            0x08, 0x04, 0xcb, 0x05,  // zh-cn
            0x08, 0x07, 0x1a, 0x75,  // de-ch
            0x08, 0x09, 0x29, 0xc5,  // en-gb
            0x08, 0x0a, 0x46, 0x75,  // es-mx
            0x08, 0x0c, 0x4e, 0xc5,  // fr-be
            0x08, 0x10, 0x68, 0x75,  // it-ch
            0x08, 0x13, 0x89, 0x85,  // nl-be
            0x08, 0x14, 0x8b, 0xc5,  // nn-no
            0x08, 0x16, 0x95, 0xe5,  // pt-pt
            0x08, 0x18, 0x9b, 0x35,  // ro-md
            0x08, 0x19, 0x9d, 0x25,  // ru-md
            0x08, 0x1a, 0xae, 0x9a,  // sr-latn-cs
            0x08, 0x1d, 0xb3, 0x05,  // sv-fi
            0x08, 0x20, 0xc0, 0x25,  // ur-in
            0x08, 0x2c, 0x0c, 0x8a,  // az-cyrl-az
            0x08, 0x2e, 0x1c, 0xd6,  // dsb-de
            0x08, 0x32, 0xba, 0x35,  // tn-bw
            0x08, 0x3b, 0xa2, 0x15,  // se-se
            0x08, 0x3c, 0x5d, 0xd5,  // ga-ie
            0x08, 0x3e, 0x84, 0x35,  // ms-bn
            0x08, 0x43, 0xc1, 0x6a,  // uz-cyrl-uz
            0x08, 0x45, 0x10, 0xd5,  // bn-bd
            0x08, 0x46, 0x90, 0x4a,  // pa-arab-pk
            0x08, 0x49, 0xb5, 0xf5,  // ta-lk
            0x08, 0x50, 0x81, 0xea,  // mn-mong-cn
            0x08, 0x59, 0xa0, 0x3a,  // sd-arab-pk
            0x08, 0x5d, 0x6a, 0x0a,  // iu-latn-ca
            0x08, 0x5f, 0xbd, 0x7b,  // tzm-latn-dz
            0x08, 0x60, 0x74, 0x4a,  // ks-deva-in
            0x08, 0x61, 0x88, 0x95,  // ne-in
            0x08, 0x67, 0x4b, 0xda,  // ff-latn-sn
            0x08, 0x6b, 0x99, 0xd6,  // quz-ec
            0x08, 0x73, 0xb8, 0xe5,  // ti-er
            0x09, 0x01, 0x96, 0xdd,  // qps-latn-x-sh
            0x09, 0xff, 0x98, 0x39,  // qps-plocm
            0x0c, 0x01, 0x04, 0x35,  // ar-eg
            0x0c, 0x04, 0xce, 0x35,  // zh-hk
            0x0c, 0x07, 0x19, 0xd5,  // de-at
            0x0c, 0x09, 0x22, 0x95,  // en-au
            0x0c, 0x0a, 0x44, 0xc5,  // es-es
            0x0c, 0x0c, 0x50, 0x55,  // fr-ca
            0x0c, 0x1a, 0xab, 0x7a,  // sr-cyrl-cs
            0x0c, 0x3b, 0xa1, 0x75,  // se-fi
            0x0c, 0x50, 0x82, 0x8a,  // mn-mong-mn
            0x0c, 0x51, 0x1e, 0x45,  // dz-bt
            0x0c, 0x6b, 0x9a, 0x36,  // quz-pe
            0x10, 0x01, 0x06, 0xb5,  // ar-ly
            0x10, 0x04, 0xd0, 0x75,  // zh-sg
            0x10, 0x07, 0x1c, 0x25,  // de-lu
            0x10, 0x09, 0x25, 0x15,  // en-ca
            0x10, 0x0a, 0x45, 0xd5,  // es-gt
            0x10, 0x0c, 0x51, 0x95,  // fr-ch
            0x10, 0x1a, 0x64, 0x15,  // hr-ba
            0x10, 0x3b, 0xa6, 0x86,  // smj-no
            0x10, 0x5f, 0xbe, 0xdb,  // tzm-tfng-ma
            0x14, 0x01, 0x03, 0xe5,  // ar-dz
            0x14, 0x04, 0xce, 0xf5,  // zh-mo
            0x14, 0x07, 0x1b, 0xd5,  // de-li
            0x14, 0x09, 0x35, 0xf5,  // en-nz
            0x14, 0x0a, 0x43, 0x85,  // es-cr
            0x14, 0x0c, 0x55, 0xa5,  // fr-lu
            0x14, 0x1a, 0x13, 0x6a,  // bs-latn-ba
            0x14, 0x3b, 0xa6, 0xe6,  // smj-se
            0x18, 0x01, 0x07, 0x05,  // ar-ma
            0x18, 0x09, 0x2c, 0xe5,  // en-ie
            0x18, 0x0a, 0x47, 0x15,  // es-pa
            0x18, 0x0c, 0x56, 0x45,  // fr-mc
            0x18, 0x1a, 0xad, 0xfa,  // sr-latn-ba
            0x18, 0x3b, 0xa5, 0xc6,  // sma-no
            0x1c, 0x01, 0x0a, 0x75,  // ar-tn
            0x1c, 0x09, 0x40, 0x45,  // en-za
            0x1c, 0x0a, 0x44, 0x25,  // es-do
            0x1c, 0x0c, 0x4e, 0x66,  // fr-029
            0x1c, 0x1a, 0xaa, 0xda,  // sr-cyrl-ba
            0x1c, 0x3b, 0xa6, 0x26,  // sma-se
            0x20, 0x01, 0x07, 0xa5,  // ar-om
            0x20, 0x09, 0x2e, 0xc5,  // en-jm
            0x20, 0x0a, 0x49, 0x95,  // es-ve
            0x20, 0x0c, 0x59, 0xb5,  // fr-re
            0x20, 0x1a, 0x12, 0xca,  // bs-cyrl-ba
            0x20, 0x3b, 0xa7, 0xa6,  // sms-fi
            0x24, 0x01, 0x0a, 0xc5,  // ar-ye
            0x24, 0x09, 0x20, 0x96,  // en-029
            0x24, 0x0a, 0x43, 0x35,  // es-co
            0x24, 0x0c, 0x50, 0xa5,  // fr-cd
            0x24, 0x1a, 0xaf, 0xda,  // sr-latn-rs
            0x24, 0x3b, 0xa7, 0x46,  // smn-fi
            0x28, 0x01, 0x09, 0xd5,  // ar-sy
            0x28, 0x09, 0x24, 0xc5,  // en-bz
            0x28, 0x0a, 0x47, 0x65,  // es-pe
            0x28, 0x0c, 0x5a, 0xa5,  // fr-sn
            0x28, 0x1a, 0xac, 0xba,  // sr-cyrl-rs
            0x2c, 0x01, 0x05, 0x75,  // ar-jo
            0x2c, 0x09, 0x3c, 0xd5,  // en-tt
            0x2c, 0x0a, 0x41, 0xf5,  // es-ar
            0x2c, 0x0c, 0x52, 0x35,  // fr-cm
            0x2c, 0x1a, 0xaf, 0x3a,  // sr-latn-me
            0x30, 0x01, 0x06, 0x65,  // ar-lb
            0x30, 0x09, 0x40, 0xe5,  // en-zw
            0x30, 0x0a, 0x44, 0x75,  // es-ec
            0x30, 0x0c, 0x51, 0xe5,  // fr-ci
            0x30, 0x1a, 0xac, 0x1a,  // sr-cyrl-me
            0x34, 0x01, 0x06, 0x15,  // ar-kw
            0x34, 0x09, 0x36, 0x95,  // en-ph
            0x34, 0x0a, 0x42, 0xe5,  // es-cl
            0x34, 0x0c, 0x57, 0x35,  // fr-ml
            0x38, 0x01, 0x02, 0xf5,  // ar-ae
            0x38, 0x09, 0x2c, 0x95,  // en-id
            0x38, 0x0a, 0x49, 0x45,  // es-uy
            0x38, 0x0c, 0x55, 0xf5,  // fr-ma
            0x3c, 0x01, 0x03, 0x45,  // ar-bh
            0x3c, 0x09, 0x2c, 0x45,  // en-hk
            0x3c, 0x0a, 0x48, 0x55,  // es-py
            0x3c, 0x0c, 0x55, 0x05,  // fr-ht
            0x40, 0x01, 0x08, 0x45,  // ar-qa
            0x40, 0x09, 0x2d, 0xd5,  // en-in
            0x40, 0x0a, 0x42, 0x45,  // es-bo
            0x44, 0x09, 0x33, 0xc5,  // en-my
            0x44, 0x0a, 0x48, 0xa5,  // es-sv
            0x48, 0x09, 0x39, 0xb5,  // en-sg
            0x48, 0x0a, 0x46, 0x25,  // es-hn
            0x4c, 0x0a, 0x46, 0xc5,  // es-ni
            0x50, 0x0a, 0x48, 0x05,  // es-pr
            0x54, 0x0a, 0x48, 0xf5,  // es-us
            0x58, 0x0a, 0x41, 0x96,  // es-419
            0x5c, 0x0a, 0x43, 0xd5,  // es-cu
            0x64, 0x1a, 0x12, 0xc7,  // bs-cyrl
            0x68, 0x1a, 0x13, 0x67,  // bs-latn
            0x6c, 0x1a, 0xaa, 0xd7,  // sr-cyrl
            0x70, 0x1a, 0xad, 0xf7,  // sr-latn
            0x70, 0x3b, 0xa7, 0x43,  // smn
            0x74, 0x2c, 0x0c, 0x87,  // az-cyrl
            0x74, 0x3b, 0xa7, 0xa3,  // sms
            0x78, 0x04, 0xca, 0x42,  // zh
            0x78, 0x14, 0x8b, 0xc2,  // nn
            0x78, 0x1a, 0x12, 0xc2,  // bs
            0x78, 0x2c, 0x0d, 0x27,  // az-latn
            0x78, 0x3b, 0xa5, 0xc3,  // sma
            0x78, 0x43, 0xc1, 0x67,  // uz-cyrl
            0x78, 0x50, 0x81, 0x27,  // mn-cyrl
            0x78, 0x5d, 0x69, 0x67,  // iu-cans
            0x78, 0x5f, 0xbe, 0xd8,  // tzm-tfng
            0x7c, 0x04, 0xca, 0xa6,  // zh-cht
            0x7c, 0x14, 0x86, 0xe2,  // nb
            0x7c, 0x1a, 0xaa, 0xd2,  // sr
            0x7c, 0x28, 0xb7, 0xf7,  // tg-cyrl
            0x7c, 0x2e, 0x1c, 0xd3,  // dsb
            0x7c, 0x3b, 0xa6, 0x83,  // smj
            0x7c, 0x43, 0xc2, 0x07,  // uz-latn
            0x7c, 0x46, 0x90, 0x47,  // pa-arab
            0x7c, 0x50, 0x81, 0xe7,  // mn-mong
            0x7c, 0x59, 0xa0, 0x37,  // sd-arab
            0x7c, 0x5c, 0x16, 0xe8,  // chr-cher
            0x7c, 0x5d, 0x6a, 0x07,  // iu-latn
            0x7c, 0x5f, 0xbd, 0x78,  // tzm-latn
            0x7c, 0x67, 0x4b, 0xd7,  // ff-latn
            0x7c, 0x68, 0x61, 0x37,  // ha-latn
            0x7c, 0x86, 0x98, 0xc8,  // quc-latn
            0x7c, 0x92, 0x76, 0x07,  // ku-arab
            // Sort 0x1
            0x00, 0x7f, 0xc6, 0x7b,  // x-iv_mathan
            0x04, 0x07, 0x1a, 0xcc,  // de-de_phoneb
            0x04, 0x0e, 0x65, 0x1c,  // hu-hu_technl
            0x04, 0x37, 0x6d, 0x6c,  // ka-ge_modern
            // Sort 0x2
            0x08, 0x04, 0xcb, 0xcc,  // zh-cn_stroke
            0x10, 0x04, 0xd1, 0x3c,  // zh-sg_stroke
            0x14, 0x04, 0xcf, 0xbc,  // zh-mo_stroke
            // Sort 0x3
            0x04, 0x04, 0xd1, 0xfc,  // zh-tw_pronun
            // Sort 0x4
            0x04, 0x04, 0xd2, 0xbc,  // zh-tw_radstr
            0x04, 0x11, 0x6a, 0xac,  // ja-jp_radstr
            0x0c, 0x04, 0xce, 0x3c,  // zh-hk_radstr
            0x14, 0x04, 0xce, 0xfc,  // zh-mo_radstr
            // Sort 0x5
            0x08, 0x04, 0xcb, 0x0c,  // zh-cn_phoneb
            0x10, 0x04, 0xd0, 0x7c,  // zh-sg_phoneb
        };

        private const int LcidSortPrefix1Index = 1736;
        private const int LcidSortPrefix2Index = 1752;
        private const int LcidSortPrefix3Index = 1764;
        private const int LcidSortPrefix4Index = 1768;
        private const int LcidSortPrefix5Index = 1784;

        // ThreeLetterWindowsLanguageName is string containing 3-letter Windows language names
        // every 3-characters entry is matching locale name entry in CultureNames
        private static ReadOnlySpan<byte> ThreeLetterWindowsLanguageName => new byte[CulturesCount * 3]
        {
            (byte)'Z', (byte)'Z', (byte)'Z',  // aa
            (byte)'Z', (byte)'Z', (byte)'Z',  // aa-dj
            (byte)'Z', (byte)'Z', (byte)'Z',  // aa-er
            (byte)'Z', (byte)'Z', (byte)'Z',  // aa-et
            (byte)'A', (byte)'F', (byte)'K',  // af
            (byte)'Z', (byte)'Z', (byte)'Z',  // af-na
            (byte)'A', (byte)'F', (byte)'K',  // af-za
            (byte)'Z', (byte)'Z', (byte)'Z',  // agq
            (byte)'Z', (byte)'Z', (byte)'Z',  // agq-cm
            (byte)'Z', (byte)'Z', (byte)'Z',  // ak
            (byte)'Z', (byte)'Z', (byte)'Z',  // ak-gh
            (byte)'A', (byte)'M', (byte)'H',  // am
            (byte)'A', (byte)'M', (byte)'H',  // am-et
            (byte)'A', (byte)'R', (byte)'A',  // ar
            (byte)'Z', (byte)'Z', (byte)'Z',  // ar-001
            (byte)'A', (byte)'R', (byte)'U',  // ar-ae
            (byte)'A', (byte)'R', (byte)'H',  // ar-bh
            (byte)'Z', (byte)'Z', (byte)'Z',  // ar-dj
            (byte)'A', (byte)'R', (byte)'G',  // ar-dz
            (byte)'A', (byte)'R', (byte)'E',  // ar-eg
            (byte)'Z', (byte)'Z', (byte)'Z',  // ar-er
            (byte)'Z', (byte)'Z', (byte)'Z',  // ar-il
            (byte)'A', (byte)'R', (byte)'I',  // ar-iq
            (byte)'A', (byte)'R', (byte)'J',  // ar-jo
            (byte)'Z', (byte)'Z', (byte)'Z',  // ar-km
            (byte)'A', (byte)'R', (byte)'K',  // ar-kw
            (byte)'A', (byte)'R', (byte)'B',  // ar-lb
            (byte)'A', (byte)'R', (byte)'L',  // ar-ly
            (byte)'A', (byte)'R', (byte)'M',  // ar-ma
            (byte)'Z', (byte)'Z', (byte)'Z',  // ar-mr
            (byte)'A', (byte)'R', (byte)'O',  // ar-om
            (byte)'Z', (byte)'Z', (byte)'Z',  // ar-ps
            (byte)'A', (byte)'R', (byte)'Q',  // ar-qa
            (byte)'A', (byte)'R', (byte)'A',  // ar-sa
            (byte)'Z', (byte)'Z', (byte)'Z',  // ar-sd
            (byte)'Z', (byte)'Z', (byte)'Z',  // ar-so
            (byte)'Z', (byte)'Z', (byte)'Z',  // ar-ss
            (byte)'A', (byte)'R', (byte)'S',  // ar-sy
            (byte)'Z', (byte)'Z', (byte)'Z',  // ar-td
            (byte)'A', (byte)'R', (byte)'T',  // ar-tn
            (byte)'A', (byte)'R', (byte)'Y',  // ar-ye
            (byte)'M', (byte)'P', (byte)'D',  // arn
            (byte)'M', (byte)'P', (byte)'D',  // arn-cl
            (byte)'A', (byte)'S', (byte)'M',  // as
            (byte)'A', (byte)'S', (byte)'M',  // as-in
            (byte)'Z', (byte)'Z', (byte)'Z',  // asa
            (byte)'Z', (byte)'Z', (byte)'Z',  // asa-tz
            (byte)'Z', (byte)'Z', (byte)'Z',  // ast
            (byte)'Z', (byte)'Z', (byte)'Z',  // ast-es
            (byte)'A', (byte)'Z', (byte)'E',  // az
            (byte)'A', (byte)'Z', (byte)'C',  // az-cyrl
            (byte)'A', (byte)'Z', (byte)'C',  // az-cyrl-az
            (byte)'A', (byte)'Z', (byte)'E',  // az-latn
            (byte)'A', (byte)'Z', (byte)'E',  // az-latn-az
            (byte)'B', (byte)'A', (byte)'S',  // ba
            (byte)'B', (byte)'A', (byte)'S',  // ba-ru
            (byte)'Z', (byte)'Z', (byte)'Z',  // bas
            (byte)'Z', (byte)'Z', (byte)'Z',  // bas-cm
            (byte)'B', (byte)'E', (byte)'L',  // be
            (byte)'B', (byte)'E', (byte)'L',  // be-by
            (byte)'Z', (byte)'Z', (byte)'Z',  // bem
            (byte)'Z', (byte)'Z', (byte)'Z',  // bem-zm
            (byte)'Z', (byte)'Z', (byte)'Z',  // bez
            (byte)'Z', (byte)'Z', (byte)'Z',  // bez-tz
            (byte)'B', (byte)'G', (byte)'R',  // bg
            (byte)'B', (byte)'G', (byte)'R',  // bg-bg
            (byte)'Z', (byte)'Z', (byte)'Z',  // bin
            (byte)'Z', (byte)'Z', (byte)'Z',  // bin-ng
            (byte)'Z', (byte)'Z', (byte)'Z',  // bm
            (byte)'Z', (byte)'Z', (byte)'Z',  // bm-latn
            (byte)'Z', (byte)'Z', (byte)'Z',  // bm-latn-ml
            (byte)'B', (byte)'N', (byte)'B',  // bn
            (byte)'B', (byte)'N', (byte)'B',  // bn-bd
            (byte)'B', (byte)'N', (byte)'G',  // bn-in
            (byte)'B', (byte)'O', (byte)'B',  // bo
            (byte)'B', (byte)'O', (byte)'B',  // bo-cn
            (byte)'Z', (byte)'Z', (byte)'Z',  // bo-in
            (byte)'B', (byte)'R', (byte)'E',  // br
            (byte)'B', (byte)'R', (byte)'E',  // br-fr
            (byte)'Z', (byte)'Z', (byte)'Z',  // brx
            (byte)'Z', (byte)'Z', (byte)'Z',  // brx-in
            (byte)'B', (byte)'S', (byte)'B',  // bs
            (byte)'B', (byte)'S', (byte)'C',  // bs-cyrl
            (byte)'B', (byte)'S', (byte)'C',  // bs-cyrl-ba
            (byte)'B', (byte)'S', (byte)'B',  // bs-latn
            (byte)'B', (byte)'S', (byte)'B',  // bs-latn-ba
            (byte)'Z', (byte)'Z', (byte)'Z',  // byn
            (byte)'Z', (byte)'Z', (byte)'Z',  // byn-er
            (byte)'C', (byte)'A', (byte)'T',  // ca
            (byte)'Z', (byte)'Z', (byte)'Z',  // ca-ad
            (byte)'C', (byte)'A', (byte)'T',  // ca-es
            (byte)'V', (byte)'A', (byte)'L',  // ca-es-valencia
            (byte)'Z', (byte)'Z', (byte)'Z',  // ca-fr
            (byte)'Z', (byte)'Z', (byte)'Z',  // ca-it
            (byte)'Z', (byte)'Z', (byte)'Z',  // ce
            (byte)'Z', (byte)'Z', (byte)'Z',  // ce-ru
            (byte)'Z', (byte)'Z', (byte)'Z',  // cgg
            (byte)'Z', (byte)'Z', (byte)'Z',  // cgg-ug
            (byte)'C', (byte)'R', (byte)'E',  // chr
            (byte)'C', (byte)'R', (byte)'E',  // chr-cher
            (byte)'C', (byte)'R', (byte)'E',  // chr-cher-us
            (byte)'C', (byte)'O', (byte)'S',  // co
            (byte)'C', (byte)'O', (byte)'S',  // co-fr
            (byte)'C', (byte)'S', (byte)'Y',  // cs
            (byte)'C', (byte)'S', (byte)'Y',  // cs-cz
            (byte)'Z', (byte)'Z', (byte)'Z',  // cu
            (byte)'Z', (byte)'Z', (byte)'Z',  // cu-ru
            (byte)'C', (byte)'Y', (byte)'M',  // cy
            (byte)'C', (byte)'Y', (byte)'M',  // cy-gb
            (byte)'D', (byte)'A', (byte)'N',  // da
            (byte)'D', (byte)'A', (byte)'N',  // da-dk
            (byte)'Z', (byte)'Z', (byte)'Z',  // da-gl
            (byte)'Z', (byte)'Z', (byte)'Z',  // dav
            (byte)'Z', (byte)'Z', (byte)'Z',  // dav-ke
            (byte)'D', (byte)'E', (byte)'U',  // de
            (byte)'D', (byte)'E', (byte)'A',  // de-at
            (byte)'Z', (byte)'Z', (byte)'Z',  // de-be
            (byte)'D', (byte)'E', (byte)'S',  // de-ch
            (byte)'D', (byte)'E', (byte)'U',  // de-de
            (byte)'D', (byte)'E', (byte)'U',  // de-de_phoneb
            (byte)'Z', (byte)'Z', (byte)'Z',  // de-it
            (byte)'D', (byte)'E', (byte)'C',  // de-li
            (byte)'D', (byte)'E', (byte)'L',  // de-lu
            (byte)'Z', (byte)'Z', (byte)'Z',  // dje
            (byte)'Z', (byte)'Z', (byte)'Z',  // dje-ne
            (byte)'D', (byte)'S', (byte)'B',  // dsb
            (byte)'D', (byte)'S', (byte)'B',  // dsb-de
            (byte)'Z', (byte)'Z', (byte)'Z',  // dua
            (byte)'Z', (byte)'Z', (byte)'Z',  // dua-cm
            (byte)'D', (byte)'I', (byte)'V',  // dv
            (byte)'D', (byte)'I', (byte)'V',  // dv-mv
            (byte)'Z', (byte)'Z', (byte)'Z',  // dyo
            (byte)'Z', (byte)'Z', (byte)'Z',  // dyo-sn
            (byte)'Z', (byte)'Z', (byte)'Z',  // dz
            (byte)'Z', (byte)'Z', (byte)'Z',  // dz-bt
            (byte)'Z', (byte)'Z', (byte)'Z',  // ebu
            (byte)'Z', (byte)'Z', (byte)'Z',  // ebu-ke
            (byte)'Z', (byte)'Z', (byte)'Z',  // ee
            (byte)'Z', (byte)'Z', (byte)'Z',  // ee-gh
            (byte)'Z', (byte)'Z', (byte)'Z',  // ee-tg
            (byte)'E', (byte)'L', (byte)'L',  // el
            (byte)'Z', (byte)'Z', (byte)'Z',  // el-cy
            (byte)'E', (byte)'L', (byte)'L',  // el-gr
            (byte)'E', (byte)'N', (byte)'U',  // en
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-001
            (byte)'E', (byte)'N', (byte)'B',  // en-029
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-150
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-ag
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-ai
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-as
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-at
            (byte)'E', (byte)'N', (byte)'A',  // en-au
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-bb
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-be
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-bi
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-bm
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-bs
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-bw
            (byte)'E', (byte)'N', (byte)'L',  // en-bz
            (byte)'E', (byte)'N', (byte)'C',  // en-ca
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-cc
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-ch
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-ck
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-cm
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-cx
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-cy
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-de
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-dk
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-dm
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-er
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-fi
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-fj
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-fk
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-fm
            (byte)'E', (byte)'N', (byte)'G',  // en-gb
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-gd
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-gg
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-gh
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-gi
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-gm
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-gu
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-gy
            (byte)'E', (byte)'N', (byte)'H',  // en-hk
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-id
            (byte)'E', (byte)'N', (byte)'I',  // en-ie
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-il
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-im
            (byte)'E', (byte)'N', (byte)'N',  // en-in
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-io
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-je
            (byte)'E', (byte)'N', (byte)'J',  // en-jm
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-ke
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-ki
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-kn
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-ky
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-lc
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-lr
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-ls
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-mg
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-mh
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-mo
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-mp
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-ms
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-mt
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-mu
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-mw
            (byte)'E', (byte)'N', (byte)'M',  // en-my
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-na
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-nf
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-ng
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-nl
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-nr
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-nu
            (byte)'E', (byte)'N', (byte)'Z',  // en-nz
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-pg
            (byte)'E', (byte)'N', (byte)'P',  // en-ph
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-pk
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-pn
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-pr
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-pw
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-rw
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-sb
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-sc
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-sd
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-se
            (byte)'E', (byte)'N', (byte)'E',  // en-sg
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-sh
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-si
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-sl
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-ss
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-sx
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-sz
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-tc
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-tk
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-to
            (byte)'E', (byte)'N', (byte)'T',  // en-tt
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-tv
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-tz
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-ug
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-um
            (byte)'E', (byte)'N', (byte)'U',  // en-us
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-vc
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-vg
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-vi
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-vu
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-ws
            (byte)'E', (byte)'N', (byte)'S',  // en-za
            (byte)'Z', (byte)'Z', (byte)'Z',  // en-zm
            (byte)'E', (byte)'N', (byte)'W',  // en-zw
            (byte)'Z', (byte)'Z', (byte)'Z',  // eo
            (byte)'Z', (byte)'Z', (byte)'Z',  // eo-001
            (byte)'E', (byte)'S', (byte)'N',  // es
            (byte)'E', (byte)'S', (byte)'J',  // es-419
            (byte)'E', (byte)'S', (byte)'S',  // es-ar
            (byte)'E', (byte)'S', (byte)'B',  // es-bo
            (byte)'Z', (byte)'Z', (byte)'Z',  // es-br
            (byte)'E', (byte)'S', (byte)'L',  // es-cl
            (byte)'E', (byte)'S', (byte)'O',  // es-co
            (byte)'E', (byte)'S', (byte)'C',  // es-cr
            (byte)'E', (byte)'S', (byte)'K',  // es-cu
            (byte)'E', (byte)'S', (byte)'D',  // es-do
            (byte)'E', (byte)'S', (byte)'F',  // es-ec
            (byte)'E', (byte)'S', (byte)'N',  // es-es
            (byte)'E', (byte)'S', (byte)'P',  // es-es_tradnl
            (byte)'Z', (byte)'Z', (byte)'Z',  // es-gq
            (byte)'E', (byte)'S', (byte)'G',  // es-gt
            (byte)'E', (byte)'S', (byte)'H',  // es-hn
            (byte)'E', (byte)'S', (byte)'M',  // es-mx
            (byte)'E', (byte)'S', (byte)'I',  // es-ni
            (byte)'E', (byte)'S', (byte)'A',  // es-pa
            (byte)'E', (byte)'S', (byte)'R',  // es-pe
            (byte)'Z', (byte)'Z', (byte)'Z',  // es-ph
            (byte)'E', (byte)'S', (byte)'U',  // es-pr
            (byte)'E', (byte)'S', (byte)'Z',  // es-py
            (byte)'E', (byte)'S', (byte)'E',  // es-sv
            (byte)'E', (byte)'S', (byte)'T',  // es-us
            (byte)'E', (byte)'S', (byte)'Y',  // es-uy
            (byte)'E', (byte)'S', (byte)'V',  // es-ve
            (byte)'E', (byte)'T', (byte)'I',  // et
            (byte)'E', (byte)'T', (byte)'I',  // et-ee
            (byte)'E', (byte)'U', (byte)'Q',  // eu
            (byte)'E', (byte)'U', (byte)'Q',  // eu-es
            (byte)'Z', (byte)'Z', (byte)'Z',  // ewo
            (byte)'Z', (byte)'Z', (byte)'Z',  // ewo-cm
            (byte)'F', (byte)'A', (byte)'R',  // fa
            (byte)'F', (byte)'A', (byte)'R',  // fa-ir
            (byte)'F', (byte)'U', (byte)'L',  // ff
            (byte)'Z', (byte)'Z', (byte)'Z',  // ff-cm
            (byte)'Z', (byte)'Z', (byte)'Z',  // ff-gn
            (byte)'F', (byte)'U', (byte)'L',  // ff-latn
            (byte)'F', (byte)'U', (byte)'L',  // ff-latn-sn
            (byte)'Z', (byte)'Z', (byte)'Z',  // ff-mr
            (byte)'Z', (byte)'Z', (byte)'Z',  // ff-ng
            (byte)'F', (byte)'I', (byte)'N',  // fi
            (byte)'F', (byte)'I', (byte)'N',  // fi-fi
            (byte)'F', (byte)'P', (byte)'O',  // fil
            (byte)'F', (byte)'P', (byte)'O',  // fil-ph
            (byte)'F', (byte)'O', (byte)'S',  // fo
            (byte)'Z', (byte)'Z', (byte)'Z',  // fo-dk
            (byte)'F', (byte)'O', (byte)'S',  // fo-fo
            (byte)'F', (byte)'R', (byte)'A',  // fr
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-029
            (byte)'F', (byte)'R', (byte)'B',  // fr-be
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-bf
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-bi
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-bj
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-bl
            (byte)'F', (byte)'R', (byte)'C',  // fr-ca
            (byte)'F', (byte)'R', (byte)'D',  // fr-cd
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-cf
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-cg
            (byte)'F', (byte)'R', (byte)'S',  // fr-ch
            (byte)'F', (byte)'R', (byte)'I',  // fr-ci
            (byte)'F', (byte)'R', (byte)'E',  // fr-cm
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-dj
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-dz
            (byte)'F', (byte)'R', (byte)'A',  // fr-fr
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-ga
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-gf
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-gn
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-gp
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-gq
            (byte)'F', (byte)'R', (byte)'H',  // fr-ht
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-km
            (byte)'F', (byte)'R', (byte)'L',  // fr-lu
            (byte)'F', (byte)'R', (byte)'O',  // fr-ma
            (byte)'F', (byte)'R', (byte)'M',  // fr-mc
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-mf
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-mg
            (byte)'F', (byte)'R', (byte)'F',  // fr-ml
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-mq
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-mr
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-mu
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-nc
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-ne
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-pf
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-pm
            (byte)'F', (byte)'R', (byte)'R',  // fr-re
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-rw
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-sc
            (byte)'F', (byte)'R', (byte)'N',  // fr-sn
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-sy
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-td
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-tg
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-tn
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-vu
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-wf
            (byte)'Z', (byte)'Z', (byte)'Z',  // fr-yt
            (byte)'Z', (byte)'Z', (byte)'Z',  // fur
            (byte)'Z', (byte)'Z', (byte)'Z',  // fur-it
            (byte)'F', (byte)'Y', (byte)'N',  // fy
            (byte)'F', (byte)'Y', (byte)'N',  // fy-nl
            (byte)'I', (byte)'R', (byte)'E',  // ga
            (byte)'I', (byte)'R', (byte)'E',  // ga-ie
            (byte)'G', (byte)'L', (byte)'A',  // gd
            (byte)'G', (byte)'L', (byte)'A',  // gd-gb
            (byte)'G', (byte)'L', (byte)'C',  // gl
            (byte)'G', (byte)'L', (byte)'C',  // gl-es
            (byte)'G', (byte)'R', (byte)'N',  // gn
            (byte)'G', (byte)'R', (byte)'N',  // gn-py
            (byte)'Z', (byte)'Z', (byte)'Z',  // gsw
            (byte)'Z', (byte)'Z', (byte)'Z',  // gsw-ch
            (byte)'G', (byte)'S', (byte)'W',  // gsw-fr
            (byte)'Z', (byte)'Z', (byte)'Z',  // gsw-li
            (byte)'G', (byte)'U', (byte)'J',  // gu
            (byte)'G', (byte)'U', (byte)'J',  // gu-in
            (byte)'Z', (byte)'Z', (byte)'Z',  // guz
            (byte)'Z', (byte)'Z', (byte)'Z',  // guz-ke
            (byte)'Z', (byte)'Z', (byte)'Z',  // gv
            (byte)'Z', (byte)'Z', (byte)'Z',  // gv-im
            (byte)'H', (byte)'A', (byte)'U',  // ha
            (byte)'H', (byte)'A', (byte)'U',  // ha-latn
            (byte)'Z', (byte)'Z', (byte)'Z',  // ha-latn-gh
            (byte)'Z', (byte)'Z', (byte)'Z',  // ha-latn-ne
            (byte)'H', (byte)'A', (byte)'U',  // ha-latn-ng
            (byte)'H', (byte)'A', (byte)'W',  // haw
            (byte)'H', (byte)'A', (byte)'W',  // haw-us
            (byte)'H', (byte)'E', (byte)'B',  // he
            (byte)'H', (byte)'E', (byte)'B',  // he-il
            (byte)'H', (byte)'I', (byte)'N',  // hi
            (byte)'H', (byte)'I', (byte)'N',  // hi-in
            (byte)'H', (byte)'R', (byte)'V',  // hr
            (byte)'H', (byte)'R', (byte)'B',  // hr-ba
            (byte)'H', (byte)'R', (byte)'V',  // hr-hr
            (byte)'H', (byte)'S', (byte)'B',  // hsb
            (byte)'H', (byte)'S', (byte)'B',  // hsb-de
            (byte)'H', (byte)'U', (byte)'N',  // hu
            (byte)'H', (byte)'U', (byte)'N',  // hu-hu
            (byte)'H', (byte)'U', (byte)'N',  // hu-hu_technl
            (byte)'H', (byte)'Y', (byte)'E',  // hy
            (byte)'H', (byte)'Y', (byte)'E',  // hy-am
            (byte)'Z', (byte)'Z', (byte)'Z',  // ia
            (byte)'Z', (byte)'Z', (byte)'Z',  // ia-001
            (byte)'Z', (byte)'Z', (byte)'Z',  // ia-fr
            (byte)'Z', (byte)'Z', (byte)'Z',  // ibb
            (byte)'Z', (byte)'Z', (byte)'Z',  // ibb-ng
            (byte)'I', (byte)'N', (byte)'D',  // id
            (byte)'I', (byte)'N', (byte)'D',  // id-id
            (byte)'I', (byte)'B', (byte)'O',  // ig
            (byte)'I', (byte)'B', (byte)'O',  // ig-ng
            (byte)'I', (byte)'I', (byte)'I',  // ii
            (byte)'I', (byte)'I', (byte)'I',  // ii-cn
            (byte)'I', (byte)'S', (byte)'L',  // is
            (byte)'I', (byte)'S', (byte)'L',  // is-is
            (byte)'I', (byte)'T', (byte)'A',  // it
            (byte)'I', (byte)'T', (byte)'S',  // it-ch
            (byte)'I', (byte)'T', (byte)'A',  // it-it
            (byte)'Z', (byte)'Z', (byte)'Z',  // it-sm
            (byte)'I', (byte)'U', (byte)'K',  // iu
            (byte)'I', (byte)'U', (byte)'S',  // iu-cans
            (byte)'I', (byte)'U', (byte)'S',  // iu-cans-ca
            (byte)'I', (byte)'U', (byte)'K',  // iu-latn
            (byte)'I', (byte)'U', (byte)'K',  // iu-latn-ca
            (byte)'J', (byte)'P', (byte)'N',  // ja
            (byte)'J', (byte)'P', (byte)'N',  // ja-jp
            (byte)'J', (byte)'P', (byte)'N',  // ja-jp_radstr
            (byte)'Z', (byte)'Z', (byte)'Z',  // jgo
            (byte)'Z', (byte)'Z', (byte)'Z',  // jgo-cm
            (byte)'Z', (byte)'Z', (byte)'Z',  // jmc
            (byte)'Z', (byte)'Z', (byte)'Z',  // jmc-tz
            (byte)'J', (byte)'A', (byte)'V',  // jv
            (byte)'Z', (byte)'Z', (byte)'Z',  // jv-java
            (byte)'Z', (byte)'Z', (byte)'Z',  // jv-java-id
            (byte)'J', (byte)'A', (byte)'V',  // jv-latn
            (byte)'J', (byte)'A', (byte)'V',  // jv-latn-id
            (byte)'K', (byte)'A', (byte)'T',  // ka
            (byte)'K', (byte)'A', (byte)'T',  // ka-ge
            (byte)'K', (byte)'A', (byte)'T',  // ka-ge_modern
            (byte)'Z', (byte)'Z', (byte)'Z',  // kab
            (byte)'Z', (byte)'Z', (byte)'Z',  // kab-dz
            (byte)'Z', (byte)'Z', (byte)'Z',  // kam
            (byte)'Z', (byte)'Z', (byte)'Z',  // kam-ke
            (byte)'Z', (byte)'Z', (byte)'Z',  // kde
            (byte)'Z', (byte)'Z', (byte)'Z',  // kde-tz
            (byte)'Z', (byte)'Z', (byte)'Z',  // kea
            (byte)'Z', (byte)'Z', (byte)'Z',  // kea-cv
            (byte)'Z', (byte)'Z', (byte)'Z',  // khq
            (byte)'Z', (byte)'Z', (byte)'Z',  // khq-ml
            (byte)'Z', (byte)'Z', (byte)'Z',  // ki
            (byte)'Z', (byte)'Z', (byte)'Z',  // ki-ke
            (byte)'K', (byte)'K', (byte)'Z',  // kk
            (byte)'K', (byte)'K', (byte)'Z',  // kk-kz
            (byte)'Z', (byte)'Z', (byte)'Z',  // kkj
            (byte)'Z', (byte)'Z', (byte)'Z',  // kkj-cm
            (byte)'K', (byte)'A', (byte)'L',  // kl
            (byte)'K', (byte)'A', (byte)'L',  // kl-gl
            (byte)'Z', (byte)'Z', (byte)'Z',  // kln
            (byte)'Z', (byte)'Z', (byte)'Z',  // kln-ke
            (byte)'K', (byte)'H', (byte)'M',  // km
            (byte)'K', (byte)'H', (byte)'M',  // km-kh
            (byte)'K', (byte)'D', (byte)'I',  // kn
            (byte)'K', (byte)'D', (byte)'I',  // kn-in
            (byte)'K', (byte)'O', (byte)'R',  // ko
            (byte)'Z', (byte)'Z', (byte)'Z',  // ko-kp
            (byte)'K', (byte)'O', (byte)'R',  // ko-kr
            (byte)'K', (byte)'N', (byte)'K',  // kok
            (byte)'K', (byte)'N', (byte)'K',  // kok-in
            (byte)'Z', (byte)'Z', (byte)'Z',  // kr
            (byte)'Z', (byte)'Z', (byte)'Z',  // kr-ng
            (byte)'Z', (byte)'Z', (byte)'Z',  // ks
            (byte)'Z', (byte)'Z', (byte)'Z',  // ks-arab
            (byte)'Z', (byte)'Z', (byte)'Z',  // ks-arab-in
            (byte)'Z', (byte)'Z', (byte)'Z',  // ks-deva
            (byte)'Z', (byte)'Z', (byte)'Z',  // ks-deva-in
            (byte)'Z', (byte)'Z', (byte)'Z',  // ksb
            (byte)'Z', (byte)'Z', (byte)'Z',  // ksb-tz
            (byte)'Z', (byte)'Z', (byte)'Z',  // ksf
            (byte)'Z', (byte)'Z', (byte)'Z',  // ksf-cm
            (byte)'Z', (byte)'Z', (byte)'Z',  // ksh
            (byte)'Z', (byte)'Z', (byte)'Z',  // ksh-de
            (byte)'K', (byte)'U', (byte)'R',  // ku
            (byte)'K', (byte)'U', (byte)'R',  // ku-arab
            (byte)'K', (byte)'U', (byte)'R',  // ku-arab-iq
            (byte)'Z', (byte)'Z', (byte)'Z',  // ku-arab-ir
            (byte)'Z', (byte)'Z', (byte)'Z',  // kw
            (byte)'Z', (byte)'Z', (byte)'Z',  // kw-gb
            (byte)'K', (byte)'Y', (byte)'R',  // ky
            (byte)'K', (byte)'Y', (byte)'R',  // ky-kg
            (byte)'Z', (byte)'Z', (byte)'Z',  // la
            (byte)'Z', (byte)'Z', (byte)'Z',  // la-001
            (byte)'Z', (byte)'Z', (byte)'Z',  // lag
            (byte)'Z', (byte)'Z', (byte)'Z',  // lag-tz
            (byte)'L', (byte)'B', (byte)'X',  // lb
            (byte)'L', (byte)'B', (byte)'X',  // lb-lu
            (byte)'Z', (byte)'Z', (byte)'Z',  // lg
            (byte)'Z', (byte)'Z', (byte)'Z',  // lg-ug
            (byte)'Z', (byte)'Z', (byte)'Z',  // lkt
            (byte)'Z', (byte)'Z', (byte)'Z',  // lkt-us
            (byte)'Z', (byte)'Z', (byte)'Z',  // ln
            (byte)'Z', (byte)'Z', (byte)'Z',  // ln-ao
            (byte)'Z', (byte)'Z', (byte)'Z',  // ln-cd
            (byte)'Z', (byte)'Z', (byte)'Z',  // ln-cf
            (byte)'Z', (byte)'Z', (byte)'Z',  // ln-cg
            (byte)'L', (byte)'A', (byte)'O',  // lo
            (byte)'L', (byte)'A', (byte)'O',  // lo-la
            (byte)'Z', (byte)'Z', (byte)'Z',  // lrc
            (byte)'Z', (byte)'Z', (byte)'Z',  // lrc-iq
            (byte)'Z', (byte)'Z', (byte)'Z',  // lrc-ir
            (byte)'L', (byte)'T', (byte)'H',  // lt
            (byte)'L', (byte)'T', (byte)'H',  // lt-lt
            (byte)'Z', (byte)'Z', (byte)'Z',  // lu
            (byte)'Z', (byte)'Z', (byte)'Z',  // lu-cd
            (byte)'Z', (byte)'Z', (byte)'Z',  // luo
            (byte)'Z', (byte)'Z', (byte)'Z',  // luo-ke
            (byte)'Z', (byte)'Z', (byte)'Z',  // luy
            (byte)'Z', (byte)'Z', (byte)'Z',  // luy-ke
            (byte)'L', (byte)'V', (byte)'I',  // lv
            (byte)'L', (byte)'V', (byte)'I',  // lv-lv
            (byte)'Z', (byte)'Z', (byte)'Z',  // mas
            (byte)'Z', (byte)'Z', (byte)'Z',  // mas-ke
            (byte)'Z', (byte)'Z', (byte)'Z',  // mas-tz
            (byte)'Z', (byte)'Z', (byte)'Z',  // mer
            (byte)'Z', (byte)'Z', (byte)'Z',  // mer-ke
            (byte)'Z', (byte)'Z', (byte)'Z',  // mfe
            (byte)'Z', (byte)'Z', (byte)'Z',  // mfe-mu
            (byte)'M', (byte)'L', (byte)'G',  // mg
            (byte)'M', (byte)'L', (byte)'G',  // mg-mg
            (byte)'Z', (byte)'Z', (byte)'Z',  // mgh
            (byte)'Z', (byte)'Z', (byte)'Z',  // mgh-mz
            (byte)'Z', (byte)'Z', (byte)'Z',  // mgo
            (byte)'Z', (byte)'Z', (byte)'Z',  // mgo-cm
            (byte)'M', (byte)'R', (byte)'I',  // mi
            (byte)'M', (byte)'R', (byte)'I',  // mi-nz
            (byte)'M', (byte)'K', (byte)'I',  // mk
            (byte)'M', (byte)'K', (byte)'I',  // mk-mk
            (byte)'M', (byte)'Y', (byte)'M',  // ml
            (byte)'M', (byte)'Y', (byte)'M',  // ml-in
            (byte)'M', (byte)'N', (byte)'N',  // mn
            (byte)'M', (byte)'N', (byte)'N',  // mn-cyrl
            (byte)'M', (byte)'N', (byte)'N',  // mn-mn
            (byte)'M', (byte)'N', (byte)'G',  // mn-mong
            (byte)'M', (byte)'N', (byte)'G',  // mn-mong-cn
            (byte)'M', (byte)'N', (byte)'M',  // mn-mong-mn
            (byte)'Z', (byte)'Z', (byte)'Z',  // mni
            (byte)'Z', (byte)'Z', (byte)'Z',  // mni-in
            (byte)'M', (byte)'W', (byte)'K',  // moh
            (byte)'M', (byte)'W', (byte)'K',  // moh-ca
            (byte)'M', (byte)'A', (byte)'R',  // mr
            (byte)'M', (byte)'A', (byte)'R',  // mr-in
            (byte)'M', (byte)'S', (byte)'L',  // ms
            (byte)'M', (byte)'S', (byte)'B',  // ms-bn
            (byte)'M', (byte)'S', (byte)'L',  // ms-my
            (byte)'Z', (byte)'Z', (byte)'Z',  // ms-sg
            (byte)'M', (byte)'L', (byte)'T',  // mt
            (byte)'M', (byte)'L', (byte)'T',  // mt-mt
            (byte)'Z', (byte)'Z', (byte)'Z',  // mua
            (byte)'Z', (byte)'Z', (byte)'Z',  // mua-cm
            (byte)'M', (byte)'Y', (byte)'A',  // my
            (byte)'M', (byte)'Y', (byte)'A',  // my-mm
            (byte)'Z', (byte)'Z', (byte)'Z',  // mzn
            (byte)'Z', (byte)'Z', (byte)'Z',  // mzn-ir
            (byte)'Z', (byte)'Z', (byte)'Z',  // naq
            (byte)'Z', (byte)'Z', (byte)'Z',  // naq-na
            (byte)'N', (byte)'O', (byte)'R',  // nb
            (byte)'N', (byte)'O', (byte)'R',  // nb-no
            (byte)'Z', (byte)'Z', (byte)'Z',  // nb-sj
            (byte)'Z', (byte)'Z', (byte)'Z',  // nd
            (byte)'Z', (byte)'Z', (byte)'Z',  // nd-zw
            (byte)'Z', (byte)'Z', (byte)'Z',  // nds
            (byte)'Z', (byte)'Z', (byte)'Z',  // nds-de
            (byte)'Z', (byte)'Z', (byte)'Z',  // nds-nl
            (byte)'N', (byte)'E', (byte)'P',  // ne
            (byte)'N', (byte)'E', (byte)'I',  // ne-in
            (byte)'N', (byte)'E', (byte)'P',  // ne-np
            (byte)'N', (byte)'L', (byte)'D',  // nl
            (byte)'Z', (byte)'Z', (byte)'Z',  // nl-aw
            (byte)'N', (byte)'L', (byte)'B',  // nl-be
            (byte)'Z', (byte)'Z', (byte)'Z',  // nl-bq
            (byte)'Z', (byte)'Z', (byte)'Z',  // nl-cw
            (byte)'N', (byte)'L', (byte)'D',  // nl-nl
            (byte)'Z', (byte)'Z', (byte)'Z',  // nl-sr
            (byte)'Z', (byte)'Z', (byte)'Z',  // nl-sx
            (byte)'Z', (byte)'Z', (byte)'Z',  // nmg
            (byte)'Z', (byte)'Z', (byte)'Z',  // nmg-cm
            (byte)'N', (byte)'O', (byte)'N',  // nn
            (byte)'N', (byte)'O', (byte)'N',  // nn-no
            (byte)'Z', (byte)'Z', (byte)'Z',  // nnh
            (byte)'Z', (byte)'Z', (byte)'Z',  // nnh-cm
            (byte)'N', (byte)'O', (byte)'R',  // no
            (byte)'N', (byte)'Q', (byte)'O',  // nqo
            (byte)'N', (byte)'Q', (byte)'O',  // nqo-gn
            (byte)'Z', (byte)'Z', (byte)'Z',  // nr
            (byte)'Z', (byte)'Z', (byte)'Z',  // nr-za
            (byte)'N', (byte)'S', (byte)'O',  // nso
            (byte)'N', (byte)'S', (byte)'O',  // nso-za
            (byte)'Z', (byte)'Z', (byte)'Z',  // nus
            (byte)'Z', (byte)'Z', (byte)'Z',  // nus-ss
            (byte)'Z', (byte)'Z', (byte)'Z',  // nyn
            (byte)'Z', (byte)'Z', (byte)'Z',  // nyn-ug
            (byte)'O', (byte)'C', (byte)'I',  // oc
            (byte)'O', (byte)'C', (byte)'I',  // oc-fr
            (byte)'O', (byte)'R', (byte)'M',  // om
            (byte)'O', (byte)'R', (byte)'M',  // om-et
            (byte)'Z', (byte)'Z', (byte)'Z',  // om-ke
            (byte)'O', (byte)'R', (byte)'I',  // or
            (byte)'O', (byte)'R', (byte)'I',  // or-in
            (byte)'Z', (byte)'Z', (byte)'Z',  // os
            (byte)'Z', (byte)'Z', (byte)'Z',  // os-ge
            (byte)'Z', (byte)'Z', (byte)'Z',  // os-ru
            (byte)'P', (byte)'A', (byte)'N',  // pa
            (byte)'P', (byte)'A', (byte)'P',  // pa-arab
            (byte)'P', (byte)'A', (byte)'P',  // pa-arab-pk
            (byte)'P', (byte)'A', (byte)'N',  // pa-in
            (byte)'Z', (byte)'Z', (byte)'Z',  // pap
            (byte)'Z', (byte)'Z', (byte)'Z',  // pap-029
            (byte)'P', (byte)'L', (byte)'K',  // pl
            (byte)'P', (byte)'L', (byte)'K',  // pl-pl
            (byte)'Z', (byte)'Z', (byte)'Z',  // prg
            (byte)'Z', (byte)'Z', (byte)'Z',  // prg-001
            (byte)'P', (byte)'R', (byte)'S',  // prs
            (byte)'P', (byte)'R', (byte)'S',  // prs-af
            (byte)'P', (byte)'A', (byte)'S',  // ps
            (byte)'P', (byte)'A', (byte)'S',  // ps-af
            (byte)'P', (byte)'T', (byte)'B',  // pt
            (byte)'P', (byte)'T', (byte)'A',  // pt-ao
            (byte)'P', (byte)'T', (byte)'B',  // pt-br
            (byte)'Z', (byte)'Z', (byte)'Z',  // pt-ch
            (byte)'Z', (byte)'Z', (byte)'Z',  // pt-cv
            (byte)'Z', (byte)'Z', (byte)'Z',  // pt-gq
            (byte)'Z', (byte)'Z', (byte)'Z',  // pt-gw
            (byte)'Z', (byte)'Z', (byte)'Z',  // pt-lu
            (byte)'Z', (byte)'Z', (byte)'Z',  // pt-mo
            (byte)'Z', (byte)'Z', (byte)'Z',  // pt-mz
            (byte)'P', (byte)'T', (byte)'G',  // pt-pt
            (byte)'Z', (byte)'Z', (byte)'Z',  // pt-st
            (byte)'Z', (byte)'Z', (byte)'Z',  // pt-tl
            (byte)'E', (byte)'N', (byte)'J',  // qps-latn-x-sh
            (byte)'E', (byte)'N', (byte)'U',  // qps-ploc
            (byte)'J', (byte)'P', (byte)'N',  // qps-ploca
            (byte)'A', (byte)'R', (byte)'A',  // qps-plocm
            (byte)'Q', (byte)'U', (byte)'T',  // quc
            (byte)'Q', (byte)'U', (byte)'T',  // quc-latn
            (byte)'Q', (byte)'U', (byte)'T',  // quc-latn-gt
            (byte)'Q', (byte)'U', (byte)'B',  // quz
            (byte)'Q', (byte)'U', (byte)'B',  // quz-bo
            (byte)'Q', (byte)'U', (byte)'E',  // quz-ec
            (byte)'Q', (byte)'U', (byte)'P',  // quz-pe
            (byte)'R', (byte)'M', (byte)'C',  // rm
            (byte)'R', (byte)'M', (byte)'C',  // rm-ch
            (byte)'Z', (byte)'Z', (byte)'Z',  // rn
            (byte)'Z', (byte)'Z', (byte)'Z',  // rn-bi
            (byte)'R', (byte)'O', (byte)'M',  // ro
            (byte)'R', (byte)'O', (byte)'D',  // ro-md
            (byte)'R', (byte)'O', (byte)'M',  // ro-ro
            (byte)'Z', (byte)'Z', (byte)'Z',  // rof
            (byte)'Z', (byte)'Z', (byte)'Z',  // rof-tz
            (byte)'R', (byte)'U', (byte)'S',  // ru
            (byte)'Z', (byte)'Z', (byte)'Z',  // ru-by
            (byte)'Z', (byte)'Z', (byte)'Z',  // ru-kg
            (byte)'Z', (byte)'Z', (byte)'Z',  // ru-kz
            (byte)'R', (byte)'U', (byte)'M',  // ru-md
            (byte)'R', (byte)'U', (byte)'S',  // ru-ru
            (byte)'Z', (byte)'Z', (byte)'Z',  // ru-ua
            (byte)'K', (byte)'I', (byte)'N',  // rw
            (byte)'K', (byte)'I', (byte)'N',  // rw-rw
            (byte)'Z', (byte)'Z', (byte)'Z',  // rwk
            (byte)'Z', (byte)'Z', (byte)'Z',  // rwk-tz
            (byte)'S', (byte)'A', (byte)'N',  // sa
            (byte)'S', (byte)'A', (byte)'N',  // sa-in
            (byte)'S', (byte)'A', (byte)'H',  // sah
            (byte)'S', (byte)'A', (byte)'H',  // sah-ru
            (byte)'Z', (byte)'Z', (byte)'Z',  // saq
            (byte)'Z', (byte)'Z', (byte)'Z',  // saq-ke
            (byte)'Z', (byte)'Z', (byte)'Z',  // sbp
            (byte)'Z', (byte)'Z', (byte)'Z',  // sbp-tz
            (byte)'S', (byte)'I', (byte)'P',  // sd
            (byte)'S', (byte)'I', (byte)'P',  // sd-arab
            (byte)'S', (byte)'I', (byte)'P',  // sd-arab-pk
            (byte)'Z', (byte)'Z', (byte)'Z',  // sd-deva
            (byte)'Z', (byte)'Z', (byte)'Z',  // sd-deva-in
            (byte)'S', (byte)'M', (byte)'E',  // se
            (byte)'S', (byte)'M', (byte)'G',  // se-fi
            (byte)'S', (byte)'M', (byte)'E',  // se-no
            (byte)'S', (byte)'M', (byte)'F',  // se-se
            (byte)'Z', (byte)'Z', (byte)'Z',  // seh
            (byte)'Z', (byte)'Z', (byte)'Z',  // seh-mz
            (byte)'Z', (byte)'Z', (byte)'Z',  // ses
            (byte)'Z', (byte)'Z', (byte)'Z',  // ses-ml
            (byte)'Z', (byte)'Z', (byte)'Z',  // sg
            (byte)'Z', (byte)'Z', (byte)'Z',  // sg-cf
            (byte)'Z', (byte)'Z', (byte)'Z',  // shi
            (byte)'Z', (byte)'Z', (byte)'Z',  // shi-latn
            (byte)'Z', (byte)'Z', (byte)'Z',  // shi-latn-ma
            (byte)'Z', (byte)'Z', (byte)'Z',  // shi-tfng
            (byte)'Z', (byte)'Z', (byte)'Z',  // shi-tfng-ma
            (byte)'S', (byte)'I', (byte)'N',  // si
            (byte)'S', (byte)'I', (byte)'N',  // si-lk
            (byte)'S', (byte)'K', (byte)'Y',  // sk
            (byte)'S', (byte)'K', (byte)'Y',  // sk-sk
            (byte)'S', (byte)'L', (byte)'V',  // sl
            (byte)'S', (byte)'L', (byte)'V',  // sl-si
            (byte)'S', (byte)'M', (byte)'B',  // sma
            (byte)'S', (byte)'M', (byte)'A',  // sma-no
            (byte)'S', (byte)'M', (byte)'B',  // sma-se
            (byte)'S', (byte)'M', (byte)'K',  // smj
            (byte)'S', (byte)'M', (byte)'J',  // smj-no
            (byte)'S', (byte)'M', (byte)'K',  // smj-se
            (byte)'S', (byte)'M', (byte)'N',  // smn
            (byte)'S', (byte)'M', (byte)'N',  // smn-fi
            (byte)'S', (byte)'M', (byte)'S',  // sms
            (byte)'S', (byte)'M', (byte)'S',  // sms-fi
            (byte)'S', (byte)'N', (byte)'A',  // sn
            (byte)'S', (byte)'N', (byte)'A',  // sn-latn
            (byte)'S', (byte)'N', (byte)'A',  // sn-latn-zw
            (byte)'S', (byte)'O', (byte)'M',  // so
            (byte)'Z', (byte)'Z', (byte)'Z',  // so-dj
            (byte)'Z', (byte)'Z', (byte)'Z',  // so-et
            (byte)'Z', (byte)'Z', (byte)'Z',  // so-ke
            (byte)'S', (byte)'O', (byte)'M',  // so-so
            (byte)'S', (byte)'Q', (byte)'I',  // sq
            (byte)'S', (byte)'Q', (byte)'I',  // sq-al
            (byte)'Z', (byte)'Z', (byte)'Z',  // sq-mk
            (byte)'Z', (byte)'Z', (byte)'Z',  // sq-xk
            (byte)'S', (byte)'R', (byte)'M',  // sr
            (byte)'S', (byte)'R', (byte)'O',  // sr-cyrl
            (byte)'S', (byte)'R', (byte)'N',  // sr-cyrl-ba
            (byte)'S', (byte)'R', (byte)'B',  // sr-cyrl-cs
            (byte)'S', (byte)'R', (byte)'Q',  // sr-cyrl-me
            (byte)'S', (byte)'R', (byte)'O',  // sr-cyrl-rs
            (byte)'Z', (byte)'Z', (byte)'Z',  // sr-cyrl-xk
            (byte)'S', (byte)'R', (byte)'M',  // sr-latn
            (byte)'S', (byte)'R', (byte)'S',  // sr-latn-ba
            (byte)'S', (byte)'R', (byte)'L',  // sr-latn-cs
            (byte)'S', (byte)'R', (byte)'P',  // sr-latn-me
            (byte)'S', (byte)'R', (byte)'M',  // sr-latn-rs
            (byte)'Z', (byte)'Z', (byte)'Z',  // sr-latn-xk
            (byte)'Z', (byte)'Z', (byte)'Z',  // ss
            (byte)'Z', (byte)'Z', (byte)'Z',  // ss-sz
            (byte)'Z', (byte)'Z', (byte)'Z',  // ss-za
            (byte)'Z', (byte)'Z', (byte)'Z',  // ssy
            (byte)'Z', (byte)'Z', (byte)'Z',  // ssy-er
            (byte)'S', (byte)'O', (byte)'T',  // st
            (byte)'Z', (byte)'Z', (byte)'Z',  // st-ls
            (byte)'S', (byte)'O', (byte)'T',  // st-za
            (byte)'S', (byte)'V', (byte)'E',  // sv
            (byte)'Z', (byte)'Z', (byte)'Z',  // sv-ax
            (byte)'S', (byte)'V', (byte)'F',  // sv-fi
            (byte)'S', (byte)'V', (byte)'E',  // sv-se
            (byte)'S', (byte)'W', (byte)'K',  // sw
            (byte)'Z', (byte)'Z', (byte)'Z',  // sw-cd
            (byte)'S', (byte)'W', (byte)'K',  // sw-ke
            (byte)'Z', (byte)'Z', (byte)'Z',  // sw-tz
            (byte)'Z', (byte)'Z', (byte)'Z',  // sw-ug
            (byte)'Z', (byte)'Z', (byte)'Z',  // swc
            (byte)'Z', (byte)'Z', (byte)'Z',  // swc-cd
            (byte)'S', (byte)'Y', (byte)'R',  // syr
            (byte)'S', (byte)'Y', (byte)'R',  // syr-sy
            (byte)'T', (byte)'A', (byte)'I',  // ta
            (byte)'T', (byte)'A', (byte)'I',  // ta-in
            (byte)'T', (byte)'A', (byte)'M',  // ta-lk
            (byte)'Z', (byte)'Z', (byte)'Z',  // ta-my
            (byte)'Z', (byte)'Z', (byte)'Z',  // ta-sg
            (byte)'T', (byte)'E', (byte)'L',  // te
            (byte)'T', (byte)'E', (byte)'L',  // te-in
            (byte)'Z', (byte)'Z', (byte)'Z',  // teo
            (byte)'Z', (byte)'Z', (byte)'Z',  // teo-ke
            (byte)'Z', (byte)'Z', (byte)'Z',  // teo-ug
            (byte)'T', (byte)'A', (byte)'J',  // tg
            (byte)'T', (byte)'A', (byte)'J',  // tg-cyrl
            (byte)'T', (byte)'A', (byte)'J',  // tg-cyrl-tj
            (byte)'T', (byte)'H', (byte)'A',  // th
            (byte)'T', (byte)'H', (byte)'A',  // th-th
            (byte)'T', (byte)'I', (byte)'R',  // ti
            (byte)'T', (byte)'I', (byte)'R',  // ti-er
            (byte)'T', (byte)'I', (byte)'E',  // ti-et
            (byte)'Z', (byte)'Z', (byte)'Z',  // tig
            (byte)'Z', (byte)'Z', (byte)'Z',  // tig-er
            (byte)'T', (byte)'U', (byte)'K',  // tk
            (byte)'T', (byte)'U', (byte)'K',  // tk-tm
            (byte)'T', (byte)'S', (byte)'N',  // tn
            (byte)'T', (byte)'S', (byte)'B',  // tn-bw
            (byte)'T', (byte)'S', (byte)'N',  // tn-za
            (byte)'Z', (byte)'Z', (byte)'Z',  // to
            (byte)'Z', (byte)'Z', (byte)'Z',  // to-to
            (byte)'T', (byte)'R', (byte)'K',  // tr
            (byte)'Z', (byte)'Z', (byte)'Z',  // tr-cy
            (byte)'T', (byte)'R', (byte)'K',  // tr-tr
            (byte)'T', (byte)'S', (byte)'O',  // ts
            (byte)'T', (byte)'S', (byte)'O',  // ts-za
            (byte)'T', (byte)'T', (byte)'T',  // tt
            (byte)'T', (byte)'T', (byte)'T',  // tt-ru
            (byte)'Z', (byte)'Z', (byte)'Z',  // twq
            (byte)'Z', (byte)'Z', (byte)'Z',  // twq-ne
            (byte)'T', (byte)'Z', (byte)'A',  // tzm
            (byte)'Z', (byte)'Z', (byte)'Z',  // tzm-arab
            (byte)'Z', (byte)'Z', (byte)'Z',  // tzm-arab-ma
            (byte)'T', (byte)'Z', (byte)'A',  // tzm-latn
            (byte)'T', (byte)'Z', (byte)'A',  // tzm-latn-dz
            (byte)'Z', (byte)'Z', (byte)'Z',  // tzm-latn-ma
            (byte)'T', (byte)'Z', (byte)'M',  // tzm-tfng
            (byte)'T', (byte)'Z', (byte)'M',  // tzm-tfng-ma
            (byte)'U', (byte)'I', (byte)'G',  // ug
            (byte)'U', (byte)'I', (byte)'G',  // ug-cn
            (byte)'U', (byte)'K', (byte)'R',  // uk
            (byte)'U', (byte)'K', (byte)'R',  // uk-ua
            (byte)'U', (byte)'R', (byte)'D',  // ur
            (byte)'U', (byte)'R', (byte)'I',  // ur-in
            (byte)'U', (byte)'R', (byte)'D',  // ur-pk
            (byte)'U', (byte)'Z', (byte)'B',  // uz
            (byte)'Z', (byte)'Z', (byte)'Z',  // uz-arab
            (byte)'Z', (byte)'Z', (byte)'Z',  // uz-arab-af
            (byte)'U', (byte)'Z', (byte)'C',  // uz-cyrl
            (byte)'U', (byte)'Z', (byte)'C',  // uz-cyrl-uz
            (byte)'U', (byte)'Z', (byte)'B',  // uz-latn
            (byte)'U', (byte)'Z', (byte)'B',  // uz-latn-uz
            (byte)'Z', (byte)'Z', (byte)'Z',  // vai
            (byte)'Z', (byte)'Z', (byte)'Z',  // vai-latn
            (byte)'Z', (byte)'Z', (byte)'Z',  // vai-latn-lr
            (byte)'Z', (byte)'Z', (byte)'Z',  // vai-vaii
            (byte)'Z', (byte)'Z', (byte)'Z',  // vai-vaii-lr
            (byte)'Z', (byte)'Z', (byte)'Z',  // ve
            (byte)'Z', (byte)'Z', (byte)'Z',  // ve-za
            (byte)'V', (byte)'I', (byte)'T',  // vi
            (byte)'V', (byte)'I', (byte)'T',  // vi-vn
            (byte)'Z', (byte)'Z', (byte)'Z',  // vo
            (byte)'Z', (byte)'Z', (byte)'Z',  // vo-001
            (byte)'Z', (byte)'Z', (byte)'Z',  // vun
            (byte)'Z', (byte)'Z', (byte)'Z',  // vun-tz
            (byte)'Z', (byte)'Z', (byte)'Z',  // wae
            (byte)'Z', (byte)'Z', (byte)'Z',  // wae-ch
            (byte)'Z', (byte)'Z', (byte)'Z',  // wal
            (byte)'Z', (byte)'Z', (byte)'Z',  // wal-et
            (byte)'W', (byte)'O', (byte)'L',  // wo
            (byte)'W', (byte)'O', (byte)'L',  // wo-sn
            (byte)'I', (byte)'V', (byte)'L',  // x-iv_mathan
            (byte)'X', (byte)'H', (byte)'O',  // xh
            (byte)'X', (byte)'H', (byte)'O',  // xh-za
            (byte)'Z', (byte)'Z', (byte)'Z',  // xog
            (byte)'Z', (byte)'Z', (byte)'Z',  // xog-ug
            (byte)'Z', (byte)'Z', (byte)'Z',  // yav
            (byte)'Z', (byte)'Z', (byte)'Z',  // yav-cm
            (byte)'Z', (byte)'Z', (byte)'Z',  // yi
            (byte)'Z', (byte)'Z', (byte)'Z',  // yi-001
            (byte)'Y', (byte)'O', (byte)'R',  // yo
            (byte)'Z', (byte)'Z', (byte)'Z',  // yo-bj
            (byte)'Y', (byte)'O', (byte)'R',  // yo-ng
            (byte)'Z', (byte)'Z', (byte)'Z',  // yue
            (byte)'Z', (byte)'Z', (byte)'Z',  // yue-hk
            (byte)'Z', (byte)'H', (byte)'G',  // zgh
            (byte)'Z', (byte)'H', (byte)'G',  // zgh-tfng
            (byte)'Z', (byte)'H', (byte)'G',  // zgh-tfng-ma
            (byte)'C', (byte)'H', (byte)'S',  // zh
            (byte)'C', (byte)'H', (byte)'S',  // zh-chs
            (byte)'C', (byte)'H', (byte)'T',  // zh-cht
            (byte)'C', (byte)'H', (byte)'S',  // zh-cn
            (byte)'C', (byte)'H', (byte)'S',  // zh-cn_phoneb
            (byte)'C', (byte)'H', (byte)'S',  // zh-cn_stroke
            (byte)'C', (byte)'H', (byte)'S',  // zh-hans
            (byte)'Z', (byte)'Z', (byte)'Z',  // zh-hans-hk
            (byte)'Z', (byte)'Z', (byte)'Z',  // zh-hans-mo
            (byte)'Z', (byte)'H', (byte)'H',  // zh-hant
            (byte)'Z', (byte)'H', (byte)'H',  // zh-hk
            (byte)'Z', (byte)'H', (byte)'H',  // zh-hk_radstr
            (byte)'Z', (byte)'H', (byte)'M',  // zh-mo
            (byte)'Z', (byte)'H', (byte)'M',  // zh-mo_radstr
            (byte)'Z', (byte)'H', (byte)'M',  // zh-mo_stroke
            (byte)'Z', (byte)'H', (byte)'I',  // zh-sg
            (byte)'Z', (byte)'H', (byte)'I',  // zh-sg_phoneb
            (byte)'Z', (byte)'H', (byte)'I',  // zh-sg_stroke
            (byte)'C', (byte)'H', (byte)'T',  // zh-tw
            (byte)'C', (byte)'H', (byte)'T',  // zh-tw_pronun
            (byte)'C', (byte)'H', (byte)'T',  // zh-tw_radstr
            (byte)'Z', (byte)'U', (byte)'L',  // zu
            (byte)'Z', (byte)'U', (byte)'L',  // zu-za
        };

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

        internal static string? LCIDToLocaleName(int culture)
        {
            uint sort = (uint)culture >> 16;
            culture = (ushort)culture;

            ReadOnlySpan<byte> indices = LcidToCultureNameIndices;

            (int start, int end) = sort switch
            {
                0 => (0, LcidSortPrefix1Index),
                1 => (LcidSortPrefix1Index, LcidSortPrefix2Index),
                2 => (LcidSortPrefix2Index, LcidSortPrefix3Index),
                3 => (LcidSortPrefix3Index, LcidSortPrefix4Index),
                4 => (LcidSortPrefix4Index, LcidSortPrefix5Index),
                5 => (LcidSortPrefix5Index, indices.Length),
                _ => default
            };

            indices = indices[start..end];

            int lo = 0;
            int hi = indices.Length / 4 - 1;

            // Binary search the array
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int index = i * 4;

                int array_value = indices[index] << 8 | indices[index + 1];

                int order = array_value.CompareTo(culture);

                if (order == 0)
                {
                    start = (indices[index + 2] << 4) | indices[index + 3] >> 4;
                    int length = indices[index + 3] & 0xF;
                    return GetString(CultureNames.Slice(start, length));
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

            Debug.Assert(CulturesCount == (ThreeLetterWindowsLanguageName.Length / 3));
            return GetString(ThreeLetterWindowsLanguageName.Slice(index * 3, 3));
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

            Debug.Assert(CulturesCount * 2 == LocalesNamesIndexes.Length);

            int lo = 0;
            int hi = CulturesCount - 1;

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
            ReadOnlySpan<byte> localesNamesIndexes = LocalesNamesIndexes;
            int index = localeNameIndice * 2;

            int high = localesNamesIndexes[index];
            int low = localesNamesIndexes[index + 1];

            int start = (high << 4) | (low >> 4);
            int length = low & 0xF;

            return CultureNames.Slice(start, length);
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
