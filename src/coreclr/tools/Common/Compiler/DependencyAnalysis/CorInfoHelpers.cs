// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.JitInterface;

namespace ILCompiler.DependencyAnalysis
{
    public static class CorInfoHelpers
    {
        public static ReadyToRunHelperId GetReadyToRunHelperFromStaticBaseHelper(CorInfoHelpFunc helper)
        {
            ReadyToRunHelperId res;
            switch (helper)
            {
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_CCTOR_TRIGGER:
                    res = ReadyToRunHelperId.CctorTrigger;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_GCSTATIC_BASE:
                    res = ReadyToRunHelperId.GetGCStaticBase;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_NONGCSTATIC_BASE:
                    res = ReadyToRunHelperId.GetNonGCStaticBase;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_THREADSTATIC_BASE:
                    res = ReadyToRunHelperId.GetThreadStaticBase;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_NONGCTHREADSTATIC_BASE:
                    res = ReadyToRunHelperId.GetThreadNonGcStaticBase;
                    break;
                default:
                    throw new NotImplementedException("ReadyToRun: " + helper.ToString());
            }
            return res;
        }
    }
}
