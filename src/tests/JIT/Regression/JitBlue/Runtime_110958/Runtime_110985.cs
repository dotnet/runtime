// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen
// Reduced from 17.57 KB to 2.06 KB.

using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class Runtime_110958
{
    long long_30 = 5;
    ulong ulong_36 = 0;
    SveMaskPattern SveMaskPattern_37 = SveMaskPattern.VectorCount1;
    private static List<string> toPrint = new List<string>();
    private void Method0()
    {
        unchecked
        {
            ulong ulong_77 = 2;
            SveMaskPattern SveMaskPattern_78 = SveMaskPattern_37;
            try
            {
                try
                {
                    ulong_36 -= ulong_77;
                }
                catch (System.FieldAccessException)
                { }
                catch (System.ExecutionEngineException)
                { }
            }
            catch (System.RankException)
            {
            }
            catch (System.MemberAccessException)
            {
                do
                {
                    try
                    {
                        switch ((15 << 4) * long_30)
                        {
                            case -1:
                                {
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                    }
                    catch (System.InvalidProgramException)
                    {
                    }
                    catch (System.InvalidCastException)
                    {
                    }
                }
                while (15 < 4);
            }
            catch (System.AggregateException)
            {
            }
            finally
            {
            }
            return;
        }
    }

    [Fact]
    public static void Problem0()
    {
        new Runtime_110958().Method0();
    }
}

