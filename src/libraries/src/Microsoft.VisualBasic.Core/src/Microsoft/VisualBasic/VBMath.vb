' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports Microsoft.VisualBasic.CompilerServices

Namespace Microsoft.VisualBasic

    Public Module VBMath

        ' Equivalent to calling VB6 rtRandomNext(1.0)
        Public Function Rnd() As Single
            Return Rnd(CSng(1))
        End Function

        ' Equivalent to VB6 rtRandomNext function
        Public Function Rnd(ByVal Number As Single) As Single
            Dim oProj As ProjectData = ProjectData.GetProjectData()
            Dim rndSeed As Integer = oProj.m_rndSeed

            '  if parameter is zero, generate float from present seed
            If (Number <> 0.0) Then
                '  if parameter is negative, use to create new seed
                If (Number < 0.0) Then
                    'Original C++ code
                    'rndSeed = *(ULONG *) & fltVal;
                    'rndSeed = (rndSeed + (rndSeed >> 24)) & 0xffffffL;

                    rndSeed = BitConverter.ToInt32(BitConverter.GetBytes(Number), 0)

                    Dim i64 As Int64 = rndSeed
                    i64 = (i64 And &HFFFFFFFFL)
                    rndSeed = CInt((i64 + (i64 >> 24)) And &HFFFFFFI)
                End If

                '  if parameter is non-zero, generate a new seed
                rndSeed = CInt((CLng(rndSeed) * &H43FD43FDL + &HC39EC3L) And &HFFFFFFL)
            End If

            '  copy back seed value to per-project structure
            oProj.m_rndSeed = rndSeed

            '  normalize seed to floating value from 0.0 up to 1.0
            Return CSng(rndSeed) / CSng(16777216.0)
        End Function

        'Equivalent to RandomizeTimer in the VB6 codebase
        Public Sub Randomize()
            Dim oProj As ProjectData = ProjectData.GetProjectData()
            Dim sngTimer As Single = GetTimer()
            Dim rndSeed As Int32 = oProj.m_rndSeed
            Dim lValue As Int32

            '  treat Single as a long Integer
            lValue = BitConverter.ToInt32(BitConverter.GetBytes(sngTimer), 0)

            '  xor the upper and lower words of the long and put in
            '  the middle two bytes
            lValue = ((lValue And &HFFFFI) Xor (lValue >> 16)) << 8

            '  replace the middle two bytes of the seed with lValue
            rndSeed = (rndSeed And &HFF0000FFI) Or lValue

            '  copy back seed value to per-project structure
            oProj.m_rndSeed = rndSeed
        End Sub

        'Equivalent to RandomizeValue in the VB6 codebase
        Public Sub Randomize(ByVal Number As Double)
            Dim rndSeed As Integer
            Dim lValue As Integer
            Dim oProj As ProjectData

            oProj = ProjectData.GetProjectData()
            rndSeed = oProj.m_rndSeed

            '  for little-endian R8, the high-order Integer is second half
            If BitConverter.IsLittleEndian Then
                lValue = BitConverter.ToInt32(BitConverter.GetBytes(Number), 4)
            Else
                lValue = BitConverter.ToInt32(BitConverter.GetBytes(Number), 0)
            End If

            '  xor the upper and lower words of the Integer and put in
            '  the middle two bytes
            ' Original C++ line
            ' lValue = ((lValue & 0xffff) ^ (lValue >> 16)) << 8;
            lValue = ((lValue And &HFFFFI) Xor (lValue >> 16)) << 8

            '  replace the middle two bytes of the seed with lValue
            'Original C++ line
            ' rndSeed = (rndSeed & 0xff0000ff) | lValue;
            rndSeed = (rndSeed And &HFF0000FFI) Or lValue

            '  copy back seed value to per-project structure
            oProj.m_rndSeed = rndSeed
        End Sub

        Private Function GetTimer() As Single
            Dim dt As Date

            dt = System.DateTime.Now
            Return CSng((60 * dt.Hour + dt.Minute) * 60 + dt.Second + (dt.Millisecond / 1000))
        End Function

    End Module

End Namespace
