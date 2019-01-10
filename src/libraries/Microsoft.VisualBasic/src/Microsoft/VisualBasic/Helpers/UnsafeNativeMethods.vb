' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Security
Imports System.Runtime.InteropServices
Imports System.Runtime.Versioning

Namespace Microsoft.VisualBasic.CompilerServices

    <ComVisible(False)>
    <SuppressUnmanagedCodeSecurityAttribute()>
    Friend NotInheritable Class UnsafeNativeMethods
        ''' <summary>
        ''' Frees memory allocated from the local heap. i.e. frees memory allocated
        ''' by LocalAlloc or LocalReAlloc.n
        ''' </summary>
        ''' <param name="LocalHandle"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <SecurityCritical()>
        <ResourceExposure(ResourceScope.None)>
        <DllImport("kernel32", ExactSpelling:=True, SetLastError:=True)>
        Friend Shared Function LocalFree(ByVal LocalHandle As IntPtr) As IntPtr
        End Function
    End Class
End Namespace
