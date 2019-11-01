' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports System.Runtime.Versioning

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    <ComVisible(False)>
    Friend NotInheritable Class _
        SafeNativeMethods

        <ResourceExposure(ResourceScope.None)>
        <PreserveSig()> Friend Declare Sub _
            GetLocalTime _
                Lib "kernel32" (ByVal systime As NativeTypes.SystemTime)

        '''*************************************************************************
        ''' ;New
        ''' <summary>
        ''' FxCop violation: Avoid uninstantiated internal class. 
        ''' Adding a private constructor to prevent the compiler from generating a default constructor.
        ''' </summary>
        Private Sub New()
        End Sub
    End Class

End Namespace

