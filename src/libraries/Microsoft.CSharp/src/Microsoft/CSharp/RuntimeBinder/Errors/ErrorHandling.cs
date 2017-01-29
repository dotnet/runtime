// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder.Errors
{
    // Collection of error reporting code. This class contains among other things ptrs to 
    // interfaces for constructing error locations (EE doesn't need locations), submitting errors 
    // (EE and compiler have different destinations), and error construction (again EE and 
    // compiler differ). Further decoupling would be enabled if the construction of substitution 
    // strings (the values that replace the placeholders in resource-defined error strings) were
    // done at the location in which the error is detected. Right now an ErrArg is constructed
    // and later is used to construct a string. I can't see any reason why the string creation
    // must be deferred, thus causing the error formatting/reporting subsystem to understand a 
    // whole host of types, many of which may not be relevant to the EE. 

    internal sealed class ErrorHandling
    {
        private readonly IErrorSink _errorSink;
        private readonly UserStringBuilder _userStringBuilder;
        private readonly CErrorFactory _errorFactory;

        // By default these DO NOT add related locations. To add a related location, pass an ErrArgRef.
        public void Error(ErrorCode id, params ErrArg[] args)
        {
            ErrorTreeArgs(id, args);
        }

        // By default these DO add related locations.
        public void ErrorRef(ErrorCode id, params ErrArgRef[] args)
        {
            ErrorTreeArgs(id, args);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // This function submits the given error to the controller, and if it's a fatal
        // error, throws the fatal exception.

        public void SubmitError(CParameterizedError error)
        {
            if (_errorSink != null)
            {
                _errorSink.SubmitError(error);
            }
        }

        private void MakeErrorLocArgs(out CParameterizedError error, ErrorCode id, ErrArg[] prgarg)
        {
            error = new CParameterizedError();
            error.Initialize(id, prgarg);
        }

        public void AddRelatedSymLoc(CParameterizedError err, Symbol sym)
        {
        }

        public void AddRelatedTypeLoc(CParameterizedError err, CType pType)
        {
        }

        private void MakeErrorTreeArgs(out CParameterizedError error, ErrorCode id, ErrArg[] prgarg)
        {
            MakeErrorLocArgs(out error, id, prgarg);
        }

        // By default these DO NOT add related locations. To add a related location, pass an ErrArgRef.

        public void MakeError(out CParameterizedError error, ErrorCode id, params ErrArg[] args)
        {
            MakeErrorTreeArgs(out error, id, args);
        }

        public ErrorHandling(
            UserStringBuilder strBldr,
            IErrorSink sink,
            CErrorFactory factory)
        {
            Debug.Assert(factory != null);

            _userStringBuilder = strBldr;
            _errorSink = sink;
            _errorFactory = factory;
        }

        private CError CreateError(ErrorCode iErrorIndex, string[] args)
        {
            return _errorFactory.CreateError(iErrorIndex, args);
        }
        private void ErrorTreeArgs(ErrorCode id, ErrArg[] prgarg)
        {
            CParameterizedError error;
            MakeErrorTreeArgs(out error, id, prgarg);
            SubmitError(error);
        }

        public CError RealizeError(CParameterizedError parameterizedError)
        {
            // Create an arg array manually using the type information in the ErrArgs.
            string[] prgpsz = new string[parameterizedError.GetParameterCount()];
            int[] prgiarg = new int[parameterizedError.GetParameterCount()];

            int ppsz = 0;
            int piarg = 0;
            int cargUnique = 0;

            _userStringBuilder.ResetUndisplayableStringFlag();

            for (int iarg = 0; iarg < parameterizedError.GetParameterCount(); iarg++)
            {
                ErrArg arg = parameterizedError.GetParameter(iarg);

                // If the NoStr bit is set we don't add it to prgpsz.
                if (0 != (arg.eaf & ErrArgFlags.NoStr))
                    continue;

                bool fUserStrings = false;

                if (!_userStringBuilder.ErrArgToString(out prgpsz[ppsz], arg, out fUserStrings))
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

            // don't ever display undisplayable strings to the user
            // if this happens we should track down the caller to not display the error
            // this should only ever occur in a cascading error situation due to
            // error tolerance
            if (_userStringBuilder.HadUndisplayableString())
            {
                return null;
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

                    ErrArg arg = parameterizedError.GetParameter(prgiarg[i]);
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
                            Debug.Assert(false, "Shouldn't be here!");
                            continue;
                    }

                    bool fMunge = false;

                    for (int j = i + 1; j < cpsz; j++)
                    {
                        if (prgiarg[j] < 0)
                            continue;
                        Debug.Assert(0 != (parameterizedError.GetParameter(prgiarg[j]).eaf & ErrArgFlags.Unique));
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

                        ErrArg arg2 = parameterizedError.GetParameter(prgiarg[j]);
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
                                Debug.Assert(false, "Shouldn't be here!");
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

            CError err = CreateError(parameterizedError.GetErrorNumber(), prgpsz);
            return err;
        }
    }
}
