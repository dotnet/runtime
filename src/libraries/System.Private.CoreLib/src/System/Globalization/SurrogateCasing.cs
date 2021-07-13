// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Globalization
{
    internal static class SurrogateCasing
    {
        // For simplicity ToUpper doesn't expect the Surrogate be formed with
        //  S = ((H - 0xD800) * 0x400) + (L - 0xDC00) + 0x10000
        // Instead it expect to have it in the form (H << 16) | L
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ToUpper(ushort h, ushort l, out ushort hr, out ushort lr)
        {
            switch (h)
            {
                case 0xD801:
                    // DESERET SMALL LETTERS 10428 ~ 1044F
                    if ((uint) (l - 0xdc28) <= (uint) (0xdc4f - 0xdc28))
                    {
                        hr = h;
                        lr = (ushort) ((l - 0xdc28) +  0xdc00);
                        return;
                    }

                    // OSAGE SMALL LETTERS 104D8 ~ 104FB
                    if ((uint) (l - 0xdcd8) <= (uint) (0xdcfb - 0xdcd8))
                    {
                        hr = h;
                        lr = (ushort) ((l - 0xdcd8) +  0xdcb0);
                        return;
                    }

                    // VITHKUQI SMALL LETTERS 10597 ~ 0xddbc
                    if ((uint) (l - 0xdd97) <= (uint) (0xddbc - 0xdd97))
                    {
                        if (l != 0xdda2 &&  l != 0xddb2 &&  l != 0xddbA) // Characters in range which not having casing.
                        {
                            hr = h;
                            lr = (ushort) ((l - 0xdd97) +  0xdd70);
                            return;
                        }
                    }
                    break;

                case 0xd803:
                    // OLD HUNGARIAN SMALL LETTERS 10CC0 ~ 10CF2
                    if ((uint) (l - 0xdcc0) <= (uint) (0xdcf2 - 0xdcc0))
                    {
                        hr = h;
                        lr = (ushort) ((l - 0xdcc0) +  0xdc80);
                        return;
                    }
                    break;

                case 0xd806:
                    // WARANG CITI SMALL LETTERS 118C0 ~ 118DF
                    if ((uint) (l - 0xdcc0) <= (uint) (0xdcdf - 0xdcc0))
                    {
                        hr = h;
                        lr = (ushort) ((l - 0xdcc0) +  0xdca0);
                        return;
                    }
                    break;

                case 0xd81b:
                    // MEDEFAIDRIN SMALL LETTERS 16E60 ~ 16E7F
                    if ((uint) (l - 0xde60) <= (uint) (0xde7f - 0xde60))
                    {
                        hr = h;
                        lr = (ushort) ((l - 0xde60) +  0xde40);
                        return;
                    }
                    break;

                case 0xd83a:
                    // ADLAM SMALL LETTERS 1E922 ~ 1E943
                    if ((uint) (l - 0xdd22) <= (uint) (0xdd43 - 0xdd22))
                    {
                        hr = h;
                        lr = (ushort) ((l - 0xdd22) +  0xdd00);
                        return;
                    }
                    break;
            }

            hr = h;
            lr = l;
        }

        // For simplicity ToUpper doesn't expect the Surrogate be formed with
        //  S = ((H - 0xD800) * 0x400) + (L - 0xDC00) + 0x10000
        // Instead it expect to have it in the form (H << 16) | L
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ToLower(ushort h, ushort l, out ushort hr, out ushort lr)
        {
            switch (h)
            {
                case 0xD801:
                    // DESERET CAPITAL LETTERS 0x10400 ~ 0x10427
                    if ((uint) (l - 0xdc00) <= (uint) (0xdc27 - 0xdc00))
                    {
                        hr = h;
                        lr = (ushort) ((l - 0xdc00) +  0xdc28);
                        return;
                    }

                    // OSAGE CAPITAL LETTERS 0x104b0 ~ 0x104d3
                    if ((uint) (l - 0xdcb0) <= (uint) (0xdcd3 - 0xdcb0))
                    {
                        hr = h;
                        lr = (ushort) ((l - 0xdcb0) +  0xdcd8);
                        return;
                    }

                    // VITHKUQI CAPITAL LETTERS 0x10570 ~ 0x10595
                    if ((uint) (l - 0xdd70) <= (uint) (0xdd95 - 0xdd70))
                    {
                        if (l != 0xdd7B &&  l != 0xdd8B &&  l != 0xdd93) // Characters in range which not having casing.
                        {
                            hr = h;
                            lr = (ushort) ((l - 0xdd70) +  0xdd97);
                            return;
                        }
                    }
                    break;

                case 0xd803:
                    // OLD HUNGARIAN CAPITAL LETTERS 0x10c80 ~ 0x10cb2
                    if ((uint) (l - 0xdc80) <= (uint) (0xdcb2 - 0xdc80))
                    {
                        hr = h;
                        lr = (ushort) ((l - 0xdc80) +  0xdcc0);
                        return;
                    }
                    break;

                case 0xd806:
                    // WARANG CITI CAPITAL LETTERS 0x118a0 ~ 0x118bf
                    if ((uint) (l - 0xdca0) <= (uint) (0xdcbf - 0xdca0))
                    {
                        hr = h;
                        lr = (ushort) ((l - 0xdca0) +  0xdcc0);
                        return;
                    }
                    break;

                case 0xd81b:
                    // MEDEFAIDRIN CAPITAL LETTERS 0x16e40 ~ 0x16e5f
                    if ((uint) (l - 0xde40) <= (uint) (0xde5f - 0xde40))
                    {
                        hr = h;
                        lr = (ushort) ((l - 0xde40) +  0xde60);
                        return;
                    }
                    break;

                case 0xd83a:
                    // ADLAM CAPITAL LETTERS 0x1e900 ~ 0x1e921
                    if ((uint) (l - 0xdd00) <= (uint) (0xdd21 - 0xdd00))
                    {
                        hr = h;
                        lr = (ushort) ((l - 0xdd00) +  0xdd22);
                        return;
                    }
                    break;
            }

            hr = h;
            lr = l;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Equal(char h1, char l1, char h2, char l2)
        {
            ToUpper(h1, l1, out ushort hr1, out ushort lr1);
            ToUpper(h2, l2, out ushort hr2, out ushort lr2);

            return hr1 == hr2 && lr1 == lr2;
        }
    }
}
