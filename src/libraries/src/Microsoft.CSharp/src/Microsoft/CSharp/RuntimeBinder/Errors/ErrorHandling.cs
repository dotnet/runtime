// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder.Errors
{
    internal static class ErrorHandling
    {
        public static RuntimeBinderException Error(ErrorCode id, params ErrArg[] args)
        {
            // Create an argument array manually using the type information in the ErrArgs.
            string[] prgpsz = new string[args.Length];
            int[] prgiarg = new int[args.Length];

            int ppsz = 0;
            int piarg = 0;
            int cargUnique = 0;

            UserStringBuilder builder = new UserStringBuilder();

            for (int iarg = 0; iarg < args.Length; iarg++)
            {
                ErrArg arg = args[iarg];

                // If the NoStr bit is set we don't add it to prgpsz.
                if (0 != (arg.eaf & ErrArgFlags.NoStr))
                    continue;


                if (!builder.ErrArgToString(out prgpsz[ppsz], arg, out bool fUserStrings))
                {
                    if (arg.eak == ErrArgKind.Int)
                    {
                        prgpsz[ppsz] = arg.n.ToString(CultureInfo.InvariantCulture);
                    }
                }
                ppsz++;

                int iargRec;
                if (!fUserStrings || 0 == (arg.eaf & ErrArgFlags.Unique))
                {
                    iargRec = -1;
                }
                else
                {
                    iargRec = iarg;
                    cargUnique++;
                }
                prgiarg[piarg] = iargRec;
                piarg++;
            }

            int cpsz = ppsz;

            if (cargUnique > 1)
            {
                // Copy the strings over to another buffer.
                string[] prgpszNew = new string[cpsz];
                Array.Copy(prgpsz, 0, prgpszNew, 0, cpsz);

                for (int i = 0; i < cpsz; i++)
                {
                    if (prgiarg[i] < 0 || prgpszNew[i] != prgpsz[i])
                        continue;

                    ErrArg arg = args[prgiarg[i]];
                    Debug.Assert(0 != (arg.eaf & ErrArgFlags.Unique) && 0 == (arg.eaf & ErrArgFlags.NoStr));

                    Symbol sym = null;
                    CType pType = null;

                    switch (arg.eak)
                    {
                        case ErrArgKind.Sym:
                            sym = arg.sym;
                            break;
                        case ErrArgKind.Type:
                            pType = arg.pType;
                            break;
                        case ErrArgKind.SymWithType:
                            sym = arg.swtMemo.sym;
                            break;
                        case ErrArgKind.MethWithInst:
                            sym = arg.mpwiMemo.sym;
                            break;
                        default:
                            Debug.Fail("Shouldn't be here!");
                            continue;
                    }

                    bool fMunge = false;

                    for (int j = i + 1; j < cpsz; j++)
                    {
                        if (prgiarg[j] < 0)
                            continue;
                        Debug.Assert(0 != (args[prgiarg[j]].eaf & ErrArgFlags.Unique));
                        if (prgpsz[i] != prgpsz[j])
                            continue;

                        // The strings are identical. If they are the same symbol, leave them alone.
                        // Otherwise, munge both strings. If j has already been munged, just make
                        // sure we munge i.
                        if (prgpszNew[j] != prgpsz[j])
                        {
                            fMunge = true;
                            continue;
                        }

                        ErrArg arg2 = args[prgiarg[j]];
                        Debug.Assert(0 != (arg2.eaf & ErrArgFlags.Unique) && 0 == (arg2.eaf & ErrArgFlags.NoStr));

                        Symbol sym2 = null;
                        CType pType2 = null;

                        switch (arg2.eak)
                        {
                            case ErrArgKind.Sym:
                                sym2 = arg2.sym;
                                break;
                            case ErrArgKind.Type:
                                pType2 = arg2.pType;
                                break;
                            case ErrArgKind.SymWithType:
                                sym2 = arg2.swtMemo.sym;
                                break;
                            case ErrArgKind.MethWithInst:
                                sym2 = arg2.mpwiMemo.sym;
                                break;
                            default:
                                Debug.Fail("Shouldn't be here!");
                                continue;
                        }

                        if (sym2 == sym && pType2 == pType && !fMunge)
                            continue;


                        prgpszNew[j] = prgpsz[j];

                        fMunge = true;
                    }

                    if (fMunge)
                    {
                        prgpszNew[i] = prgpsz[i];
                    }
                }

                prgpsz = prgpszNew;
            }

            return new RuntimeBinderException(string.Format(CultureInfo.InvariantCulture, ErrorFacts.GetMessage(id), prgpsz));
        }
    }
}
