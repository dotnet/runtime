// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Cheat codes
//
// L   - loop
// TC  - try catch (catch exits loop)
// TfC - try filter catch
// TF  - try finally
// x   - has padding between loop head and try entry
// c   - catch continues loop
// m   - multiple try exits (TF will remain a try finally)
// g   - giant finally (TF will remain try finally)
// p   - regions are serial, not nested
// TFi - try finally with what follows in the finally
// 
// x: we currently cannot clone loops where the try is the first thing
// as the header and preheader are different regions

public class LoopsWithEH
{
    static int[] data;
    static int n;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffect() { }

    static LoopsWithEH()
    {
        data = new int[100];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = i;
        }

        n = data[20];
    }

    [Fact]
    public static int Test_LTC() => Sum_LTC(data, n) - 90;

    public static int Sum_LTC(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            try
            {
                sum += data[i];
            }
            catch (Exception)
            {
                return -1;
            }
        }
        return sum;
    }

    [Fact]
    public static int Test_LTfC() => Sum_LTfC(data, n) - 90;

    public static int Sum_LTfC(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            try
            {
                sum += data[i];
            }
            catch (Exception) when (n > 0)
            {
                return -1;
            }
        }
        return sum;
    }

    [Fact]
    public static int Test_LxTC() => Sum_LxTC(data, n) - 110;

    public static int Sum_LxTC(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                sum += data[i];
            }
            catch (Exception)
            {
                return -1;
            }
        }
        return sum;
    }

    [Fact]
    public static int Test_LxTCc() => Sum_LxTCc(data, n) - 110;

    public static int Sum_LxTCc(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                sum += data[i];
            }
            catch (Exception)
            {
                sum += 1;
            }
        }
        return sum;
    }

    [Fact]
    public static int Test_LxTfC() => Sum_LxTfC(data, n) - 110;

    public static int Sum_LxTfC(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                sum += data[i];
            }
            catch (Exception) when (n > 0)
            {
                return -1;
            }
        }
        return sum;
    }

    [Fact]
    public static int Test_LxTfCc() => Sum_LxTfCc(data, n) - 110;

    public static int Sum_LxTfCc(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                sum += data[i];
            }
            catch (Exception) when (n > 0)
            {
                sum += 1;
            }
        }
        return sum;
    }

    [Fact]
    public static int Test_LxTCC() => Sum_LxTCC(data, n) - 110;

    public static int Sum_LxTCC(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                sum += data[i];
            }
            catch (IndexOutOfRangeException)
            {
                return -1;
            }
            catch (Exception)
            {
                return -2;
            }
        }
        return sum;
    }

    [Fact]
    public static int Test_LxTCcC() => Sum_LxTCcC(data, n) - 110;

    public static int Sum_LxTCcC(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                sum += data[i];
            }
            catch (IndexOutOfRangeException)
            {
                sum += 1;
            }
            catch (Exception)
            {
                return -2;
            }
        }
        return sum;
    }

    [Fact]
    public static int Test_LxTCCc() => Sum_LxTCCc(data, n) - 110;

    public static int Sum_LxTCCc(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                sum += data[i];
            }
            catch (IndexOutOfRangeException)
            {
                return -1;
            }
            catch (Exception)
            {
                sum += 2;
            }
        }
        return sum;
    }

    [Fact]
    public static int Test_LxTCcCc() => Sum_LxTCcCc(data, n) - 110;

    public static int Sum_LxTCcCc(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                sum += data[i];
            }
            catch (IndexOutOfRangeException)
            {
                sum += 1;
            }
            catch (Exception)
            {
                sum += 2;
            }
        }
        return sum;
    }

    [Fact]
    public static int Test_LxTCpTC() => Sum_LxTCpTC(data, n) - 300;

    public static int Sum_LxTCpTC(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                sum += data[i];
            }
            catch (Exception)
            {
                return -1;
            }

            try
            {
                sum += data[i];
            }
            catch (Exception)
            {
                return -2;
            }

        }
        return sum;
    }

    [Fact]
    public static int Test_LxTCcpTC() => Sum_LxTCcpTC(data, n) - 300;

    public static int Sum_LxTCcpTC(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                sum += data[i];
            }
            catch (Exception)
            {
                sum += 1;
            }

            try
            {
                sum += data[i];
            }
            catch (Exception)
            {
                return -2;
            }

        }
        return sum;
    }

    [Fact]
    public static int Test_LxTCpTCc() => Sum_LxTCpTCc(data, n) - 300;

    public static int Sum_LxTCpTCc(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                sum += data[i];
            }
            catch (Exception)
            {
                return -1;
            }

            try
            {
                sum += data[i];
            }
            catch (Exception)
            {
                sum += 1;
            }

        }
        return sum;
    }

    [Fact]
    public static int Test_LxTCcpTCc() => Sum_LxTCcpTCc(data, n) - 300;

    public static int Sum_LxTCcpTCc(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                sum += data[i];
            }
            catch (Exception)
            {
                sum += 2;
            }

            try
            {
                sum += data[i];
            }
            catch (Exception)
            {
                sum += 1;
            }

        }
        return sum;
    }

    [Fact]
    public static int Test_LxTF() => Sum_LxTF(data, n) - 130;

    public static int Sum_LxTF(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                sum += data[i];
            }
            finally
            {
                sum += 1;
            }
        }
        return sum;
    }

    [Fact]
    public static int Test_LxTFm() => Sum_LxTFm(data, n) - 1;

    public static int Sum_LxTFm(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                sum += data[i];

                if (sum > 100) return 101;
            }
            finally
            {
                sum += 1;
            }
        }
        return sum;
    }

    [Fact]
    public static int Test_LxTFg() => Sum_LxTFg(data, n) - 1;

    public static int Sum_LxTFg(int[] data, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                sum += data[i];

                if (sum > 100) return 101;
            }
            finally
            {
                sum += 1; sum *= 4; sum -= 1; sum /= 4;
                sum += 1; sum *= 4; sum -= 1; sum /= 4;
                sum += 1; sum *= 4; sum -= 1; sum /= 4;
                sum += 1; sum *= 4; sum -= 1; sum /= 4;
                sum += 1; sum *= 4; sum -= 1; sum /= 4;
                sum += 1; sum *= 4; sum -= 1; sum /= 4;
                sum += 1; sum *= 4; sum -= 1; sum /= 4;
                sum += 1; sum *= 4; sum -= 1; sum /= 4;
            }
        }
        return sum;
    }

    [Fact]
    public static int Test_TCLxTC() => Sum_TCLxTC(data, n) - 110;

    public static int Sum_TCLxTC(int[] data, int n)
    {
        int sum = 0;
        try
        {
            for (int i = 0; i < n; i++)
            {
                sum += 1;
                try
                {
                    sum += data[i];
                }
                catch (Exception)
                {
                    return -1;
                }
            }
        }
        catch (Exception)
        {
            return -1;
        }
        return sum;
    }

    [Fact]
    public static int Test_TCLxTCc() => Sum_TCLxTCc(data, n) - 110;

    public static int Sum_TCLxTCc(int[] data, int n)
    {
        int sum = 0;
        try
        {
            for (int i = 0; i < n; i++)
            {
                sum += 1;
                try
                {
                    sum += data[i];
                }
                catch (Exception)
                {
                    sum += 1;
                }
            }
        }
        catch (Exception)
        {
            return -1;
        }
        return sum;
    }

    [Fact]
    public static int Test_TCLxTfC() => Sum_TCLxTfC(data, n) - 110;

    public static int Sum_TCLxTfC(int[] data, int n)
    {
        int sum = 0;
        try
        {
            for (int i = 0; i < n; i++)
            {
                sum += 1;
                try
                {
                    sum += data[i];
                }
                catch (Exception) when (n > 0)
                {
                    return -1;
                }
            }
        }
        catch (Exception)
        {
            return -1;
        }
        return sum;
    }

    [Fact]
    public static int Test_TfCLxTC() => Sum_TfCLxTC(data, n) - 110;

    public static int Sum_TfCLxTC(int[] data, int n)
    {
        int sum = 0;
        try
        {
            for (int i = 0; i < n; i++)
            {
                sum += 1;
                try
                {
                    sum += data[i];
                }
                catch (Exception)
                {
                    return -1;
                }
            }
        }
        catch (Exception) when (n > 0)
        {
            return -1;
        }
        return sum;
    }

    [Fact]
    public static int Test_TfCLxTCc() => Sum_TfCLxTCc(data, n) - 110;

    public static int Sum_TfCLxTCc(int[] data, int n)
    {
        int sum = 0;
        try
        {
            for (int i = 0; i < n; i++)
            {
                sum += 1;
                try
                {
                    sum += data[i];
                }
                catch (Exception)
                {
                    sum += 1;
                }
            }
        }
        catch (Exception) when (n > 0)
        {
            return -1;
        }
        return sum;
    }

    [Fact]
    public static int Test_TfCLxTfC() => Sum_TfCLxTfC(data, n) - 110;

    public static int Sum_TfCLxTfC(int[] data, int n)
    {
        int sum = 0;
        try
        {
            for (int i = 0; i < n; i++)
            {
                sum += 1;
                try
                {
                    sum += data[i];
                }
                catch (Exception) when (n > 0)
                {
                    return -1;
                }
            }
        }
        catch (Exception) when (n > 0)
        {
            return -1;
        }
        return sum;
    }

    [Fact]
    public static int Test_TCLxTF() => Sum_TCLxTF(data, n) - 130;

    public static int Sum_TCLxTF(int[] data, int n)
    {
        int sum = 0;
        try
        {
            for (int i = 0; i < n; i++)
            {
                sum += 1;
                try
                {
                    sum += data[i];
                }
                finally
                {
                    sum += 1;
                }
            }
        }
        catch (Exception)
        {
            return -1;
        }
        return sum;
    }

    [Fact]
    public static int Test_LxTCTF() => Sum_LxTCTF(data, n) - 130;

    public static int Sum_LxTCTF(int[] data, int n)
    {
        int sum = 0;

        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                try
                {
                    sum += data[i];
                }
                finally
                {
                    sum += 1;
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        return sum;
    }

    [Fact]
    public static int Test_LxTCcTF() => Sum_LxTCcTF(data, n) - 130;

    public static int Sum_LxTCcTF(int[] data, int n)
    {
        int sum = 0;

        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                try
                {
                    sum += data[i];
                }
                finally
                {
                    sum += 1;
                }
            }
            catch (Exception)
            {
                sum += 2;
            }
        }

        return sum;
    }

    [Fact]
    public static int Test_LxTFTC() => Sum_LxTFTC(data, n) - 130;

    public static int Sum_LxTFTC(int[] data, int n)
    {
        int sum = 0;

        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                try
                {
                    sum += data[i];
                }
                catch (Exception)
                {
                    return -1;
                }
            }
            finally
            {
                sum += 1;
            }
        }

        return sum;
    }

    [Fact]
    public static int Test_LxTFTCc() => Sum_LxTFTCc(data, n) - 130;

    public static int Sum_LxTFTCc(int[] data, int n)
    {
        int sum = 0;

        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                try
                {
                    sum += data[i];
                }
                catch (Exception)
                {
                    sum += 2;
                }
            }
            finally
            {
                sum += 1;
            }
        }

        return sum;
    }

    [Fact]
    public static int Test_LxTFTF() => Sum_LxTFTF(data, n) - 110;

    public static int Sum_LxTFTF(int[] data, int n)
    {
        int sum = 0;

        for (int i = 0; i < n; i++)
        {
            sum += 1;
            try
            {
                try
                {
                    sum += data[i];
                }
                finally
                {
                    sum += -1;
                }
            }
            finally
            {
                sum += 1;
            }
        }

        return sum;
    }

    [Fact]
    public static int Test_LxTFxTF() => Sum_LxTFTF(data, n) - 110;

    public static int Sum_TFLxTF(int[] data, int n)
    {
        int sum = 0;
        try
        {
            for (int i = 0; i < n; i++)
            {
                sum += 1;
                try
                {
                    sum += data[i];
                }
                finally
                {
                    sum += 1;
                }
            }
        }
        finally
        {
            sum += 1;
        }
        return sum;
    }

    [Fact]
    public static int Test_TFTFLxTF() => Sum_TFTFLxTF(data, n) - 132;

    public static int Sum_TFTFLxTF(int[] data, int n)
    {
        int sum = 0;
        try
        {
            try
            {
                for (int i = 0; i < n; i++)
                {
                    sum += 1;
                    try
                    {
                        sum += data[i];
                    }
                    finally
                    {
                        sum += 1;
                    }
                }
            }
            finally
            {
                sum += 1;
            }
        }
        finally
        {
            sum += 1;
        }
        return sum;
    }

    [Fact]
    public static int Test_TCTFLxTF() => Sum_TCTFLxTF(data, n) - 131;

    public static int Sum_TCTFLxTF(int[] data, int n)
    {
        int sum = 0;
        try
        {
            try
            {
                for (int i = 0; i < n; i++)
                {
                    sum += 1;
                    try
                    {
                        sum += data[i];
                    }
                    finally
                    {
                        sum += 1;
                    }
                }
            }
            finally
            {
                sum += 1;
            }
        }
        catch (Exception)
        {
            return -1;
        }
        return sum;
    }

    [Fact]
    public static int Test_TFTCLxTF() => Sum_TCTFLxTF(data, n) - 131;

    public static int Sum_TFTCLxTF(int[] data, int n)
    {
        int sum = 0;
        try
        {
            try
            {
                for (int i = 0; i < n; i++)
                {
                    sum += 1;
                    try
                    {
                        sum += data[i];
                    }
                    finally
                    {
                        sum += 1;
                    }
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }
        finally
        {
            sum += 1;
        }
        return sum;
    }

    [Fact]
    public static int Test_TFiL() => Sum_TFiL(data, n) - 91;

    public static int Sum_TFiL(int[] data, int n)
    {
        int sum = 0;
        try
        {
            SideEffect();
        }
        finally
        {
            sum += 1;
            for (int i = 0; i < n; i++)
            {
                sum += data[i];
            }
        }

        return sum;
    }

    [Fact]
    public static int Test_TFiLxTF() => Sum_TFiLxTF(data, n) - 131;

    public static int Sum_TFiLxTF(int[] data, int n)
    {
        int sum = 0;
        try
        {
            SideEffect();
        }
        finally
        {
            sum += 1;
            for (int i = 0; i < n; i++)
            {
                sum += 1;
                try
                {
                    sum += data[i];
                }
                finally
                {
                    sum += 1;
                }
            }
        }

        return sum;
    }

    [Fact]
    public static int Test_TFiLxTCc() => Sum_TFiLxTCc(data, n) - 111;

    public static int Sum_TFiLxTCc(int[] data, int n)
    {
        int sum = 0;
        try
        {
            SideEffect();
        }
        finally
        {
            sum += 1;
            for (int i = 0; i < n; i++)
            {
                sum += 1;
                try
                {
                    sum += data[i];
                }
                catch (Exception)
                {
                    sum += 1;
                }
            }
        }

        return sum;
    }

    [Fact]
    public static int Test_TFiLxTC() => Sum_TFiLxTC(data, n) - 112;

    public static int Sum_TFiLxTC(int[] data, int n)
    {
        int sum = 0;
        try
        {
            SideEffect();
        }
        finally
        {
            sum += 1;
            for (int i = 0; i < n; i++)
            {
                sum += 1;
                try
                {
                    sum += data[i];
                }
                catch (Exception)
                {
                    goto after_loop;
                }
            }

        after_loop:
            sum += 1;

        }

        return sum;
    }

    [Fact]
    public static int Test_TFTFiLxTC() => Sum_TFTFiLxTC(data, n) - 113;

    public static int Sum_TFTFiLxTC(int[] data, int n)
    {
        int sum = 0;
        try
        {
            try
            {
                SideEffect();
            }
            finally
            {
                sum += 1;
                for (int i = 0; i < n; i++)
                {
                    sum += 1;
                    try
                    {
                        sum += data[i];
                    }
                    catch (Exception)
                    {
                        goto after_loop;
                    }
                }

            after_loop:
                sum += 1;
            }
        }
        finally
        {
            sum += 1;
        }

        return sum;
    }


    [Fact]
    public static int Test_TFiTFxL() => Sum_TFiTFxL(data, n) - 92;

    public static int Sum_TFiTFxL(int[] data, int n)
    {
        int sum = 0;

        try
        {
            SideEffect();
        }
        finally
        {
            try
            {
                sum += 1;
                for (int i = 0; i < n; i++)
                {
                    sum += data[i];
                }
            }
            finally
            {
                sum += 1;
            }
        }

        return sum;
    }
}

