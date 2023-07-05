' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.Math

Namespace Microsoft.VisualBasic

    Public Module Financial

        '============================================================================
        ' Financial functions.
        '============================================================================

        Private Const cnL_IT_STEP As Double = 0.00001
        Private Const cnL_IT_EPSILON As Double = 0.0000001

        '-------------------------------------------------------------
        '
        '  Name          : DDB

        '  Purpose       : Calculates depreciation for a period based on the
        '                  doubly-declining balance method. Returns the result.
        '                  It raises an error if parameters are invalid.

        '  Derivation    : At each period, 2*balance/nper is subtracted
        '                  from the balance.  The balance starts at the purchase
        '                  value, and the total of the payments may not
        '                  exceed (value - salvage).  The algorithm uses a non-
        '                  iterative method to calculate the payment.
        '                  Note that only the integral values of nper and per make any
        '                  sense.  However, Excel allowed non-integral values, and
        '                  thus these routines also work with non-integral input.
        '
        '                  PMT = 2 * (value / nper) * ( (nper -2) / nper ) ^ (per - 1)
        '
        '                  total = value * ( 1 - ( (nper - 2) / nper ) ^ per )
        '
        '                  excess = total - (value - salvage)
        '
        '                  ddb =  PMT        : if  excess <= 0
        '                  PMT-excess : if  PMT >= excess > 0
        '                  0          : if  excess > PMT
        '  Returns       : Double
        '
        '-------------------------------------------------------------
        '
        Public Function DDB(ByVal Cost As Double, ByVal Salvage As Double, ByVal Life As Double, ByVal Period As Double, Optional ByVal Factor As Double = 2.0) As Double

            Dim dRet As Double
            Dim dTot As Double
            Dim dExcess As Double
            Dim dTemp As Double
            Dim dNTemp As Double

            '   Handle invalid parameters
            If Factor <= 0.0# OrElse Salvage < 0.0# OrElse Period <= 0.0# OrElse Period > Life Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Factor"))
            End If

            '   Handle special (trivial) cases
            If Cost <= 0.0# Then
                Return 0.0#
            End If

            If Life < 2.0# Then
                Return (Cost - Salvage)
            End If

            If Life = 2.0# AndAlso Period > 1.0# Then
                Return 0.0#
            End If

            If Life = 2.0# AndAlso Period <= 1.0# Then
                Return (Cost - Salvage)
            End If

            If Period <= 1.0# Then
                dRet = Cost * Factor / Life
                dTemp = Cost - Salvage
                If dRet > dTemp Then
                    Return dTemp
                Else
                    Return dRet
                End If
            End If

            '   Perform the calculation
            dTemp = (Life - Factor) / Life
            dNTemp = Period - 1.0#

            '   WARSI Using the exponent operator for pow(..) in C code of DDB. Still got
            '   to make sure that they (pow and ^) are same for all conditions
            dRet = Factor * Cost / Life * dTemp ^ dNTemp

            '   WARSI Using the exponent operator for pow(..) in C code of DDB. Still got
            '   to make sure that they (pow and ^) are same for all conditions
            dTot = Cost * (1 - dTemp ^ Period)
            dExcess = dTot - Cost + Salvage

            If dExcess > 0.0# Then
                dRet = dRet - dExcess
            End If

            If dRet >= 0.0# Then
                DDB = dRet
            Else
                DDB = 0.0#
            End If

        End Function

        '-------------------------------------------------------------
        '
        '  Name                      : FV
        '  Purpose                   : It is computed as -

        '                                                   (1+rate)^nper - 1
        '       fv = -pv*(1+rate)^nper - PMT*(1+rate*type)* -----------------
        '                                                         rate
        '
        '       fv = -pv - PMT * nper        : if rate == 0
        '
        '
        '  Returns                   : Double
        '
        '-------------------------------------------------------------
        '
        Public Function FV(ByVal Rate As Double, ByVal NPer As Double, ByVal Pmt As Double, Optional ByVal PV As Double = 0, Optional ByVal Due As DueDate = DueDate.EndOfPeriod) As Double
            Return FV_Internal(Rate, NPer, Pmt, PV, Due)
        End Function

        Private Function FV_Internal(ByVal Rate As Double, ByVal NPer As Double, ByVal Pmt As Double, Optional ByVal PV As Double = 0, Optional ByVal Due As DueDate = DueDate.EndOfPeriod) As Double
            Dim dTemp As Double
            Dim dTemp2 As Double
            Dim dTemp3 As Double

            'Performing calculation
            If Rate = 0.0# Then
                Return (-PV - Pmt * NPer)
            End If

            If Due <> DueDate.EndOfPeriod Then
                dTemp = 1.0# + Rate
            Else
                dTemp = 1.0#
            End If

            dTemp3 = 1.0# + Rate
            dTemp2 = System.Math.Pow(dTemp3, NPer)

            'Do divides before multiplies to avoid OverflowExceptions
            Return ((-PV) * dTemp2) - ((Pmt / Rate) * dTemp * (dTemp2 - 1.0#))
        End Function

        '-------------------------------------------------------------
        '
        '  Name       : IPmt
        '  Purpose    : This function calculates the interest part of a
        '               payment for a given period.  The payment is part of
        '               a series of regular payments described by the other
        '               arguments.  The value is returned.  The function
        '               Raises an exception if params are invalid. This function
        '               calls FV and PMT. It calculates value of annuity (FV) at the
        '               beginning of period for which IPMT is desired. Since interest
        '               rate is constant FV*rate would give IPMT.
        '
        '               if type = 1 and per = 1 then IPMT = 0.
        '
        '               if (type = 0 ) IPMT = FV(per-1)*rate
        '               else IPMT = FV(per-2)*rate
        '  Returns    : Double
        '
        '-------------------------------------------------------------
        '
        Public Function IPmt(ByVal Rate As Double, ByVal Per As Double, ByVal NPer As Double, ByVal PV As Double, Optional ByVal FV As Double = 0, Optional ByVal Due As DueDate = DueDate.EndOfPeriod) As Double

            Dim Pmt As Double
            Dim dTFv As Double
            Dim dTemp As Double

            If Due <> DueDate.EndOfPeriod Then
                dTemp = 2.0#
            Else
                dTemp = 1.0#
            End If

            '   Type = 0 or non-zero only. Offset to calculate FV
            If (Per <= 0.0#) OrElse (Per >= NPer + 1) Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Per"))
            End If

            If (Due <> DueDate.EndOfPeriod) AndAlso (Per = 1.0#) Then
                Return 0.0#
            End If

            '   Calculate PMT (i.e. annuity) for given parms. Rqrd for FV
            Pmt = PMT_Internal(Rate, NPer, PV, FV, Due)

            '   Calculate FV just before the period on which interest would be applied
            If Due <> DueDate.EndOfPeriod Then
                PV = PV + Pmt
            End If

            dTFv = FV_Internal(Rate, (Per - dTemp), Pmt, PV, DueDate.EndOfPeriod)

            Return (dTFv * Rate)

        End Function

        '-------------------------------------------------------------
        '
        '  Name                      : IRR
        '  Purpose                   : This function uses an iterative procedure to find
        '                              the Internal Rate of Return of an investment.  The algorithm
        '                              basically uses the secant method to find a rate for which the
        '                              NPV of the cash flow is 0.
        '                              This function raises an exception if the parameters are invalid.
        '
        '                              This routine uses a slightly different version of the secant
        '                              routine in Rate.  The basic changes are:
        '                                   - uses LDoNPV to get the 'Y-value'
        '                                   - does not allow Rate to go below -1.
        '                                      (if the Rate drops below -1, it is forced above again)
        '                                   - has a double condition for termination:
        '                                      NPV = 0 (within L_IT_EPSILON)
        '                                      Rate1 - Rate0  approaches zero (rate is converging)
        '                                   This last does not parallel Excel, but avoids a class of
        '                                   spurious answers.  Otherwise, performance is comparable to
        '                                   Excel's, and accuracy is often better.
        '
        '  Returns                   : Double
        '
        '-------------------------------------------------------------
        '
        Public Function IRR(ByRef ValueArray() As Double, Optional ByVal Guess As Double = 0.1) As Double

            Dim dTemp As Double
            Dim dRate0 As Double
            Dim dRate1 As Double
            Dim dNPv0 As Double
            Dim dNpv1 As Double
            Dim dNpvEpsilon As Double
            Dim dTemp1 As Double
            Dim lIndex As Integer
            Dim lCVal As Integer
            Dim lUpper As Integer

            'Compiler assures that rank of ValueArray is always 1, no need to check it.
            'WARSI Check for error codes returned by UBound. Check if they match with C code
            Try   'Needed to catch dynamic arrays which have not been constructed yet.
                lUpper = ValueArray.GetUpperBound(0)
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "ValueArray"))
            End Try

            lCVal = lUpper + 1

            'Function fails for invalid parameters
            If Guess <= (-1.0#) Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Guess"))
            End If

            If lCVal <= 1 Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "ValueArray"))
            End If

            'We scale the epsilon depending on cash flow values. It is necessary
            'because even in max accuracy case where diff is in 16th digit it
            'would get scaled up.
            If ValueArray(0) > 0.0# Then
                dTemp = ValueArray(0)
            Else
                dTemp = -ValueArray(0)
            End If

            For lIndex = 0 To lUpper
                'Get max of values in cash flow
                If ValueArray(lIndex) > dTemp Then
                    dTemp = ValueArray(lIndex)
                ElseIf (-ValueArray(lIndex)) > dTemp Then
                    dTemp = -ValueArray(lIndex)
                End If
            Next lIndex

            dNpvEpsilon = dTemp * cnL_IT_EPSILON * 0.01

            'Set up the initial values for the secant method
            dRate0 = Guess
            dNPv0 = OptPV2(ValueArray, dRate0)

            If dNPv0 > 0.0# Then
                dRate1 = dRate0 + cnL_IT_STEP
            Else
                dRate1 = dRate0 - cnL_IT_STEP
            End If

            If dRate1 <= (-1.0#) Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Rate"))
            End If

            dNpv1 = OptPV2(ValueArray, dRate1)

            For lIndex = 0 To 39
                If dNpv1 = dNPv0 Then
                    If dRate1 > dRate0 Then
                        dRate0 = dRate0 - cnL_IT_STEP
                    Else
                        dRate0 = dRate0 + cnL_IT_STEP
                    End If
                    dNPv0 = OptPV2(ValueArray, dRate0)
                    If dNpv1 = dNPv0 Then
                        Throw New ArgumentException(SR.Argument_InvalidValue)
                    End If
                End If

                dRate0 = dRate1 - (dRate1 - dRate0) * dNpv1 / (dNpv1 - dNPv0)

                'Secant method of generating next approximation
                If dRate0 <= (-1.0#) Then
                    dRate0 = (dRate1 - 1.0#) * 0.5
                End If

                'Basically give the algorithm a second chance. Helps the
                'algorithm when it starts to diverge to -ve side
                dNPv0 = OptPV2(ValueArray, dRate0)
                If dRate0 > dRate1 Then
                    dTemp = dRate0 - dRate1
                Else
                    dTemp = dRate1 - dRate0
                End If

                If dNPv0 > 0.0# Then
                    dTemp1 = dNPv0
                Else
                    dTemp1 = -dNPv0
                End If

                'Test : npv - > 0 and rate converges
                If dTemp1 < dNpvEpsilon AndAlso dTemp < cnL_IT_EPSILON Then
                    Return dRate0
                End If

                'Exchange the values - store the new values in the 1's
                dTemp = dNPv0
                dNPv0 = dNpv1
                dNpv1 = dTemp
                dTemp = dRate0
                dRate0 = dRate1
                dRate1 = dTemp
            Next lIndex

            Throw New ArgumentException(SR.Argument_InvalidValue)
        End Function

        '-------------------------------------------------------------
        '
        '  Name                      : MIRR
        '  Returns                   : Double
        '
        '-------------------------------------------------------------
        '
        Public Function MIRR(ByRef ValueArray() As Double, ByVal FinanceRate As Double, ByVal ReinvestRate As Double) As Double

            Dim dNpvPos As Double
            Dim dNpvNeg As Double
            Dim dTemp As Double
            Dim dTemp1 As Double
            Dim dNTemp2 As Double
            Dim lCVal As Integer
            Dim lLower As Integer
            Dim lUpper As Integer

            If ValueArray.Rank <> 1 Then
                Throw New ArgumentException(SR.Format(SR.Argument_RankEQOne1, "ValueArray"))
            End If

            lLower = 0
            lUpper = ValueArray.GetUpperBound(0)
            lCVal = lUpper - lLower + 1

            If FinanceRate = -1.0# Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "FinanceRate"))
            End If

            If ReinvestRate = -1.0# Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "ReinvestRate"))
            End If

            If lCVal <= 1 Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "ValueArray"))
            End If

            dNpvNeg = LDoNPV(FinanceRate, ValueArray, -1)
            If dNpvNeg = 0.0# Then
                Throw New DivideByZeroException(SR.Financial_CalcDivByZero)
            End If

            dNpvPos = LDoNPV(ReinvestRate, ValueArray, 1) ' npv of +ve values
            dTemp1 = ReinvestRate + 1.0#
            dNTemp2 = lCVal

            dTemp = -dNpvPos * dTemp1 ^ dNTemp2 / (dNpvNeg * (FinanceRate + 1.0#))

            If dTemp < 0.0# Then
                Throw New ArgumentException(SR.Argument_InvalidValue)
            End If

            dTemp1 = 1 / (lCVal - 1.0#)

            MIRR = dTemp ^ dTemp1 - 1.0#

        End Function

        '-------------------------------------------------------------
        '
        '  Name                      : NPer
        '  Purpose                   :
        '
        '                           -fv + PMT*(1+rate*type) / rate
        '           (1+rate)^nper = ------------------------------
        '                            pv + PMT*(1+rate*type) / rate
        '
        '               this yields the log expression used in this function.
        '
        '               nper = (-fv - pv) / PMT     : if rate == 0
        '
        '  Returns                   : Double
        '
        '-------------------------------------------------------------
        '
        Public Function NPer(ByVal Rate As Double, ByVal Pmt As Double, ByVal PV As Double, Optional ByVal FV As Double = 0, Optional ByVal Due As DueDate = DueDate.EndOfPeriod) As Double

            Dim dTemp3 As Double
            Dim dTempFv As Double
            Dim dTempPv As Double
            Dim dTemp4 As Double

            '   Checking Error Conditions
            If Rate <= -1.0# Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Rate"))
            End If

            If Rate = 0.0# Then
                If Pmt = 0.0# Then
                    Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Pmt"))
                End If
                Return (-(PV + FV) / Pmt)
            Else
                If Due <> 0 Then
                    dTemp3 = Pmt * (1.0# + Rate) / Rate
                Else
                    dTemp3 = Pmt / Rate
                End If
                dTempFv = -FV + dTemp3
                dTempPv = PV + dTemp3

                '       Make sure the values fit the domain of log()
                If dTempFv < 0.0# AndAlso dTempPv < 0.0# Then
                    dTempFv = -1 * dTempFv
                    dTempPv = -1 * dTempPv
                ElseIf dTempFv <= 0.0# OrElse dTempPv <= 0.0# Then
                    Throw New ArgumentException(SR.Financial_CannotCalculateNPer)
                End If

                dTemp4 = Rate + 1.0#
                Return (Log(dTempFv) - Log(dTempPv)) / Log(dTemp4)
            End If

        End Function

        '-------------------------------------------------------------
        '
        '  Name       : NPV
        '  Purpose                   :
        '               This function calculates the Net Present Value of a series of
        '               payments at a given rate.  It uses LDoNPV to get the value.  No
        '               real work is done here, just some error checking.
        '               As with the others, this function puts its status in *lpwStatus,
        '               and returns the result as a double.
        '
        '                     Value1       Value2      Value3
        '               npv = -------- + ---------- + ---------- + ... for the series...
        '                     (1+rate)   (1+rate)^2   (1+rate)^3
        '
        '
        '  Returns    : Double
        '
        '-------------------------------------------------------------
        '
        Public Function NPV(ByVal Rate As Double, ByRef ValueArray() As Double) As Double

            Dim lCVal As Integer
            Dim lLower As Integer
            Dim lUpper As Integer

            If (ValueArray Is Nothing) Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidNullValue1, "ValueArray"))
            End If

            If ValueArray.Rank <> 1 Then
                Throw New ArgumentException(SR.Format(SR.Argument_RankEQOne1, "ValueArray"))
            End If

            lLower = 0
            lUpper = ValueArray.GetUpperBound(0)
            lCVal = lUpper - lLower + 1

            If Rate = (-1.0#) Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Rate"))
            End If
            If lCVal < 1 Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "ValueArray"))
            End If

            NPV = LDoNPV(Rate, ValueArray, 0)

        End Function

        '-------------------------------------------------------------
        '
        '  Name                      : PMT
        '  Purpose                   :
        '               This function, together with the four following
        '               it (Pv, Fv, NPer and Rate), can calculate
        '               a certain value associated with a regular series of
        '               equal-sized payments.  This series can be fully described
        '               by these values:
        '                     Pv   - present value
        '                     Fv   - future value (at end of series)
        '                     PMT  - the regular payment
        '                     nPer - the number of 'periods' over which the
        '                            money is paid
        '                     Rate - the interest rate per period.
        '                            (type - payments at beginning (1) or end (0) of
        '                            the period).
        '               Each function can determine one of the values, given the others.
        '
        '               General Function for the above values:
        '
        '                                                      (1+rate)^nper - 1
        '               pv * (1+rate)^nper + PMT*(1+rate*type)*----------------- + fv  = 0
        '                                                            rate
        '               rate == 0  ->  pv + PMT*nper + fv = 0
        '
        '               Thus:
        '                     (-fv - pv*(1+rate)^nper) * rate
        '               PMT = -------------------------------------
        '                     (1+rate*type) * ( (1+rate)^nper - 1 )
        '
        '               PMT = (-fv - pv) / nper    : if rate == 0
        '
        '
        '  Returns                   : Double
        '
        '-------------------------------------------------------------
        '
        Public Function Pmt(ByVal Rate As Double, ByVal NPer As Double, ByVal PV As Double, Optional ByVal FV As Double = 0, Optional ByVal Due As DueDate = DueDate.EndOfPeriod) As Double
            Return PMT_Internal(Rate, NPer, PV, FV, Due)
        End Function

        Private Function PMT_Internal(ByVal Rate As Double, ByVal NPer As Double, ByVal PV As Double, Optional ByVal FV As Double = 0, Optional ByVal Due As DueDate = DueDate.EndOfPeriod) As Double
            Dim dTemp As Double
            Dim dTemp2 As Double
            Dim dTemp3 As Double

            '       Checking for error conditions
            If NPer = 0.0# Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "NPer"))
            End If

            If Rate = 0.0# Then
                Return ((-FV - PV) / NPer)
            Else
                If Due <> 0 Then
                    dTemp = 1.0# + Rate
                Else
                    dTemp = 1.0#
                End If
                dTemp3 = Rate + 1.0#
                '       WARSI Using the exponent operator for pow(..) in C code of PMT. Still got
                '       to make sure that they (pow and ^) are same for all conditions
                dTemp2 = dTemp3 ^ NPer
                Return ((-FV - PV * dTemp2) / (dTemp * (dTemp2 - 1.0#)) * Rate)
            End If

        End Function

        '-------------------------------------------------------------
        '
        '  Name                      : PPmt
        '  Purpose                   : This function calculates the principal part of a
        '                              payment for a given period.
        '
        '                              Since PMT = IPMT +PPMT therefore
        '                                    PPMT = PMT - IPMT
        '
        '  Returns                   : Double
        '
        '-------------------------------------------------------------
        '
        Public Function PPmt(ByVal Rate As Double, ByVal Per As Double, ByVal NPer As Double, ByVal PV As Double, Optional ByVal FV As Double = 0, Optional ByVal Due As DueDate = DueDate.EndOfPeriod) As Double

            Dim Pmt As Double
            Dim dIPMT As Double

            '   Checking for error conditions
            If (Per <= 0.0#) OrElse (Per >= (NPer + 1)) Then
                Throw New ArgumentException(SR.Format(SR.PPMT_PerGT0AndLTNPer, "Per"))
            End If

            Pmt = PMT_Internal(Rate, NPer, PV, FV, Due)
            dIPMT = IPmt(Rate, Per, NPer, PV, FV, Due)

            Return (Pmt - dIPMT)

        End Function

        '-------------------------------------------------------------
        '
        '  Name       : PV
        '  Purpose    :
        '                    -fv - PMT * (1+rate*type) * ( (1+rate)^nper-1) / rate
        '               pv = -----------------------------------------------------
        '                                   (1 + rate) ^ nper
        '
        '               pv = -fv - PMT * nper     : if rate == 0
        '  Returns    : Double
        '
        '-------------------------------------------------------------
        '
        Public Function PV(ByVal Rate As Double, ByVal NPer As Double, ByVal Pmt As Double, Optional ByVal FV As Double = 0, Optional ByVal Due As DueDate = DueDate.EndOfPeriod) As Double

            Dim dTemp As Double
            Dim dTemp2 As Double
            Dim dTemp3 As Double

            If Rate = 0.0# Then
                Return (-FV - Pmt * NPer)
            Else
                If Due <> 0 Then
                    dTemp = 1.0# + Rate
                Else
                    dTemp = 1.0#
                End If
                dTemp3 = 1.0# + Rate

                '       WARSI Using the exponent operator for pow(..) in C code of PV. Still got
                '       to make sure that they (pow and ^) are same for all conditions
                dTemp2 = dTemp3 ^ NPer

                'Do divides before multiplies to avoid OverFlowExceptions
                Return (-(FV + Pmt * dTemp * ((dTemp2 - 1.0#) / Rate)) / dTemp2)
            End If

        End Function

        '-------------------------------------------------------------
        '
        '  Name       : Rate
        '  Purpose    :
        '               See PMT, above, for general details.  This
        '               function is not as simple as the others.  Due to the
        '               nature of the equation that links the 5 values (see
        '               Excel manual - PV), it is not practical to solve for
        '               rate algebraically.  As a result, this function implements
        '               the secant method of approximation.  LEvalRate provides
        '               the 'Y-values', for given rates.
        '
        '               Basic secant method:
        '               determine Rate0 and Rate1.  Use LEvalRate to get Y0, Y1.
        '
        '                                                    Y0
        '               Rate2 = Rate1 - (Rate1 - Rate0) * ---------
        '                                                 (Y1 - Y0)
        '
        '               Get Y2 from Rate2, LEvalRate.  move 1->0, 2->1 and repeat.
        '
        '               stop when abs( Yn ) < L_IT_EPSILON
        '
        '
        '  Returns    : Double
        '
        '-------------------------------------------------------------
        '
        Public Function Rate(ByVal NPer As Double, ByVal Pmt As Double, ByVal PV As Double, Optional ByVal FV As Double = 0, Optional ByVal Due As DueDate = DueDate.EndOfPeriod, Optional ByVal Guess As Double = 0.1) As Double

            Dim dTemp As Double
            Dim dRate0 As Double
            Dim dRate1 As Double
            Dim dY0 As Double
            Dim dY1 As Double
            Dim I As Integer

            '   Check for error condition
            If NPer <= 0.0# Then
                Throw New ArgumentException(SR.Rate_NPerMustBeGTZero)
            End If

            dRate0 = Guess
            dY0 = LEvalRate(dRate0, NPer, Pmt, PV, FV, Due)
            If dY0 > 0 Then
                dRate1 = (dRate0 / 2)
            Else
                dRate1 = (dRate0 * 2)
            End If

            dY1 = LEvalRate(dRate1, NPer, Pmt, PV, FV, Due)

            For I = 0 To 39
                If dY1 = dY0 Then
                    If dRate1 > dRate0 Then
                        dRate0 = dRate0 - cnL_IT_STEP
                    Else
                        dRate0 = dRate0 - cnL_IT_STEP * (-1)
                    End If
                    dY0 = LEvalRate(dRate0, NPer, Pmt, PV, FV, Due)
                    If dY1 = dY0 Then
                        Throw New ArgumentException(SR.Financial_CalcDivByZero)
                    End If
                End If

                dRate0 = dRate1 - (dRate1 - dRate0) * dY1 / (dY1 - dY0)

                '       Secant method of generating next approximation
                dY0 = LEvalRate(dRate0, NPer, Pmt, PV, FV, Due)
                If Abs(dY0) < cnL_IT_EPSILON Then
                    Return dRate0
                End If

                dTemp = dY0
                dY0 = dY1
                dY1 = dTemp
                dTemp = dRate0
                dRate0 = dRate1
                dRate1 = dTemp
            Next I

            Throw New ArgumentException(SR.Financial_CannotCalculateRate)

        End Function

        '-------------------------------------------------------------
        '
        '  Name                      : SLN
        '  Purpose                   : It calculates the depreciation by the straight
        '                              line method and returns the result. It raises
        '                              an error if parameters are invalid.
        '
        '                                                       sln = (value - salvage) / nper
        '
        '  Returns                   : Double
        '
        '-------------------------------------------------------------
        '
        Public Function SLN(ByVal Cost As Double, ByVal Salvage As Double, ByVal Life As Double) As Double

            If Life = 0.0# Then
                Throw New ArgumentException(SR.Financial_LifeNEZero)
            End If

            Return (Cost - Salvage) / (Life)

        End Function

        '-------------------------------------------------------------
        '
        '  Name       : SYD
        '  Purpose    : Calculates depreciation for a period by the
        '               sum-of-years-digits method. The result is returned.
        '               It raises an error if parameters are invalid.
        '
        '                                                                               2
        '               syd = (value - salvage) (nper - per + 1) * ------------
        '                                                                         (nper)(nper + 1)
        '
        '  Derivation : The value of the asset is divided into even parts.
        '               The first period gets N, the second gets N-1, the last
        '               gets 1.
        '  Returns    : Double
        '
        '-------------------------------------------------------------
        '
        Public Function SYD(ByVal Cost As Double, ByVal Salvage As Double, ByVal Life As Double, ByVal Period As Double) As Double

            Dim Result As Double

            If Salvage < 0.0# Then
                Throw New ArgumentException(SR.Format(SR.Financial_ArgGEZero1, "Salvage"))
            End If
            If Period > Life Then
                Throw New ArgumentException(SR.Financial_PeriodLELife)
            End If
            If Period <= 0.0# Then
                Throw New ArgumentException(SR.Format(SR.Financial_ArgGTZero1, "Period"))
            End If

            'Avoid OverflowExceptions by dividing before multiplying
            Result = (Cost - Salvage) / (Life * (Life + 1))
            Return (Result * (Life + 1 - Period) * 2)

        End Function

        '-------------------------------------------------------------
        '
        '  Name       : LEvalRate
        '  Purpose    : A local helper function. Does a useful calculation
        '               for Rate.  The function is derived from the General
        '               formulation noted above (PMT).
        '  Returns    : Double
        '
        '-------------------------------------------------------------
        '
        Private Function LEvalRate(ByVal Rate As Double, ByVal NPer As Double, ByVal Pmt As Double, ByVal PV As Double, ByVal dFv As Double, ByVal Due As DueDate) As Double

            Dim dTemp1 As Double
            Dim dTemp2 As Double
            Dim dTemp3 As Double

            If Rate = 0.0# Then
                Return (PV + Pmt * NPer + dFv)
            Else
                dTemp3 = Rate + 1.0#
                '       WARSI Using the exponent operator for pow(..) in C code of LEvalRate. Still got
                '       to make sure that they (pow and ^) are same for all conditions
                dTemp1 = dTemp3 ^ NPer

                If Due <> 0 Then
                    dTemp2 = 1 + Rate
                Else
                    dTemp2 = 1.0#
                End If
                Return (PV * dTemp1 + Pmt * dTemp2 * (dTemp1 - 1) / Rate + dFv)
            End If

        End Function

        '-------------------------------------------------------------
        '
        '  Name                      : LDoNPV
        '  Purpose                   :
        '          This function performs NPV calculations for NPV,
        '          MIRR, and IRR.  The wNType variable is used to set
        '          the type of calculation: 0 -> do all values,
        '                                   1 -> only values > 0,
        '                                  -1 -> only values < 0.
        '          Note the array pointer, lpdblVal, is preceded by the count of
        '          the entries.
        '          Since this is just an internal-use function, no fancy exports
        '          are done.  It assumes that error checking is done by the caller.
        '
        '            Value1      Value2       Value3
        '     npv = -------- + ---------- + ---------- + ... for the series...
        '           (1+rate)   (1+rate)^2   (1+rate)^3
        '
        '  Returns                   : Double
        '
        '-------------------------------------------------------------
        '

        Private Function LDoNPV(ByVal Rate As Double, ByRef ValueArray() As Double, ByVal iWNType As Integer) As Double

            Dim bSkipPos As Boolean
            Dim bSkipNeg As Boolean

            Dim dTemp2 As Double
            Dim dTotal As Double
            Dim dTVal As Double
            Dim I As Integer
            Dim lLower As Integer
            Dim lUpper As Integer

            bSkipPos = iWNType < 0
            bSkipNeg = iWNType > 0

            dTemp2 = 1.0#
            dTotal = 0.0#

            lLower = 0
            lUpper = ValueArray.GetUpperBound(0)

            For I = lLower To lUpper
                dTVal = ValueArray(I)
                dTemp2 = dTemp2 + dTemp2 * Rate

                If Not ((bSkipPos AndAlso dTVal > 0.0#) OrElse (bSkipNeg AndAlso dTVal < 0.0#)) Then
                    dTotal = dTotal + dTVal / dTemp2
                End If
            Next I

            LDoNPV = dTotal

        End Function

        '------------------------------------------------------------------------------------------------------
        ' Optimized version of PV2
        '------------------------------------------------------------------------------------------------------

        Private Function OptPV2(ByRef ValueArray() As Double, Optional ByVal Guess As Double = 0.1) As Double

            Dim lUpper, lLower, lIndex As Integer

            lLower = 0
            lUpper = ValueArray.GetUpperBound(0)

            Dim dTotal As Double = 0.0
            Dim divRate As Double = 1.0 + Guess

            While lLower <= lUpper AndAlso ValueArray(lLower) = 0.0
                lLower = lLower + 1
            End While

            For lIndex = lUpper To lLower Step -1
                dTotal = dTotal / divRate
                dTotal = dTotal + ValueArray(lIndex)
            Next lIndex
            Return dTotal

        End Function

    End Module

End Namespace
