' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On
Option Explicit On 

Imports System
Imports System.Reflection
Imports System.Diagnostics
Imports System.Collections.ObjectModel
Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils

Namespace Microsoft.VisualBasic.ApplicationServices

    '''**************************************************************************
    ''' ;AssemblyInfo
    ''' <summary>
    '''  A class that contains the information about an Application. This information can be
    '''  specified using the assembly attributes (contained in AssemblyInfo.vb file in case of
    '''  a VB project in Visual Studio .NET).
    ''' </summary>
    ''' <remarks>This class is based on the FileVersionInfo class of the framework, but
    ''' reduced to a number of relevant properties.</remarks>
    Public Class AssemblyInfo

        '= PUBLIC =============================================================

        '''**************************************************************************
        ''' ;New
        ''' <summary>
        ''' Create an AssemblyInfo from an assembly
        ''' </summary>
        ''' <param name="CurrentAssembly">The assembly for which we want to obtain the information.</param>
        Public Sub New(ByVal currentAssembly As System.Reflection.Assembly)
            If currentAssembly Is Nothing Then
                Throw GetArgumentNullException("CurrentAssembly")
            End If
            m_Assembly = currentAssembly
        End Sub

        ' NOTE: All properties below work in Low Trust Zone.

        '''**************************************************************************
        ''' ;Description
        ''' <summary>
        ''' Get the description associated with the assembly.
        ''' </summary>
        ''' <value>A String containing the AssemblyDescriptionAttribute associated with the assembly.</value>
        ''' <exception cref="System.InvalidOperationException">if the AssemblyDescriptionAttribute is not defined.</exception>
        Public ReadOnly Property Description() As String
            Get
                If m_Description Is Nothing Then
                    Dim Attribute As AssemblyDescriptionAttribute =
                        CType(GetAttribute(GetType(AssemblyDescriptionAttribute)), AssemblyDescriptionAttribute)
                    If Attribute Is Nothing Then
                        m_Description = ""
                    Else
                        m_Description = Attribute.Description
                    End If
                End If
                Return m_Description
            End Get
        End Property

        '''**************************************************************************
        ''' ;CompanyName
        ''' <summary>
        ''' Get the company name associated with the assembly.
        ''' </summary>
        ''' <value>A String containing the AssemblyCompanyAttribute associated with the assembly.</value>
        ''' <exception cref="System.InvalidOperationException">if the AssemblyCompanyAttribute is not defined.</exception>
        Public ReadOnly Property CompanyName() As String
            Get
                If m_CompanyName Is Nothing Then
                    Dim Attribute As AssemblyCompanyAttribute =
                        CType(GetAttribute(GetType(AssemblyCompanyAttribute)), AssemblyCompanyAttribute)
                    If Attribute Is Nothing Then
                        m_CompanyName = ""
                    Else
                        m_CompanyName = Attribute.Company
                    End If
                End If
                Return m_CompanyName
            End Get
        End Property

        '''**************************************************************************
        ''' ;Title
        ''' <summary>
        ''' Get the title associated with the assembly.
        ''' </summary>
        ''' <value>A String containing the AssemblyTitleAttribute associated with the assembly.</value>
        ''' <exception cref="System.InvalidOperationException">if the AssemblyTitleAttribute is not defined.</exception>
        Public ReadOnly Property Title() As String
            Get
                If m_Title Is Nothing Then
                    Dim Attribute As AssemblyTitleAttribute =
                        CType(GetAttribute(GetType(AssemblyTitleAttribute)), AssemblyTitleAttribute)
                    If Attribute Is Nothing Then
                        m_Title = ""
                    Else
                        m_Title = Attribute.Title
                    End If
                End If
                Return m_Title
            End Get
        End Property

        '''**************************************************************************
        ''' ;Copyright
        ''' <summary>
        ''' Get the copyright notices associated with the assembly.
        ''' </summary>
        ''' <value>A String containing the AssemblyCopyrightAttribute associated with the assembly.</value>
        ''' <exception cref="System.InvalidOperationException">if the AssemblyCopyrightAttribute is not defined.</exception>
        Public ReadOnly Property Copyright() As String
            Get
                If m_Copyright Is Nothing Then
                    Dim Attribute As AssemblyCopyrightAttribute = CType(GetAttribute(GetType(AssemblyCopyrightAttribute)), AssemblyCopyrightAttribute)
                    If Attribute Is Nothing Then
                        m_Copyright = ""
                    Else
                        m_Copyright = Attribute.Copyright
                    End If
                End If
                Return m_Copyright
            End Get
        End Property

        '''**************************************************************************
        ''' ;Trademark
        ''' <summary>
        ''' Get the trademark notices associated with the assembly.
        ''' </summary>
        ''' <value>A String containing the AssemblyTrademarkAttribute associated with the assembly.</value>
        ''' <exception cref="System.InvalidOperationException">if the AssemblyTrademarkAttribute is not defined.</exception>
        Public ReadOnly Property Trademark() As String
            Get
                If m_Trademark Is Nothing Then
                    Dim Attribute As AssemblyTrademarkAttribute = CType(GetAttribute(GetType(AssemblyTrademarkAttribute)), AssemblyTrademarkAttribute)
                    If Attribute Is Nothing Then
                        m_Trademark = ""
                    Else
                        m_Trademark = Attribute.Trademark
                    End If
                End If
                Return m_Trademark
            End Get
        End Property

        '''**************************************************************************
        ''' ;ProductName
        ''' <summary>
        ''' Get the product name associated with the assembly.
        ''' </summary>
        ''' <value>A String containing the AssemblyProductAttribute associated with the assembly.</value>
        ''' <exception cref="System.InvalidOperationException">if the AssemblyProductAttribute is not defined.</exception>
        Public ReadOnly Property ProductName() As String
            Get
                If m_ProductName Is Nothing Then
                    Dim Attribute As AssemblyProductAttribute = CType(GetAttribute(GetType(AssemblyProductAttribute)), AssemblyProductAttribute)
                    If Attribute Is Nothing Then
                        m_ProductName = ""
                    Else
                        m_ProductName = Attribute.Product
                    End If
                End If
                Return m_ProductName
            End Get
        End Property

        '''**************************************************************************
        ''' ;Version
        ''' <summary>
        ''' Get the version number of the assembly.
        ''' </summary>
        ''' <value>A System.Version class containing the version number of the assembly</value>
        ''' <remarks>Cannot use AssemblyVersionAttribute since it always return Nothing.</remarks>
        Public ReadOnly Property Version() As System.Version
            Get
                Return m_Assembly.GetName().Version
            End Get
        End Property

        '''**************************************************************************
        ''' ;AssemblyName
        ''' <summary>
        ''' Get the name of the file containing the manifest (usually the .exe file).
        ''' </summary>
        ''' <value>A String containing the file name.</value>
        Public ReadOnly Property AssemblyName() As String
            Get
                Return m_Assembly.GetName.Name
            End Get
        End Property

        '''**************************************************************************
        ''' ;DirectoryPath
        ''' <summary>
        ''' Gets the directory where the assembly lives.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>If you are calling this from an EXE, gives you the directory path of the exe assembly.  If you
        ''' call this from a DLL, it gives you the directory path of the DLL assembly</remarks>
        Public ReadOnly Property DirectoryPath() As String
            Get
                Return IO.Path.GetDirectoryName(m_Assembly.Location)
            End Get
        End Property

        '''******************************************************************************
        ''' ;LoadedAssemblies
        ''' <summary>
        ''' Returns the names of all assemblies loaded by the current application.
        ''' </summary>
        ''' <value>A ReadOnlyCollection(Of Assembly) containing all the loaded assemblies.</value>
        ''' <exception cref="System.AppDomainUnloadedException">attempt on an unloaded application domain.</exception>
        Public ReadOnly Property LoadedAssemblies() As ReadOnlyCollection(Of Reflection.Assembly)
            Get
                Dim Result As New Collection(Of Reflection.Assembly)
                For Each Assembly As Reflection.Assembly In AppDomain.CurrentDomain.GetAssemblies()
                    Result.Add(Assembly)
                Next
                Return New ReadOnlyCollection(Of Reflection.Assembly)(Result)
            End Get
        End Property

        '''******************************************************************************
        ''' ;StackTrace
        ''' <summary>
        ''' Returns the current stack trace information.
        ''' </summary>
        ''' <value>A string containing stack trace information. Value can be String.Empty.</value>
        ''' <exception cref="System.ArgumentOutOfRangeException">The requested stack trace information is out of range.</exception>
        Public ReadOnly Property StackTrace() As String
            Get
                Return Environment.StackTrace
            End Get
        End Property

        '''******************************************************************************
        ''' ;WorkingSet
        ''' <summary>
        ''' Gets the amount of physical memory mapped to the process context.
        ''' </summary>
        ''' <value>
        ''' A 64-bit signed integer containing the size of physical memory mapped to the process context, in bytes.
        ''' </value>
        Public ReadOnly Property WorkingSet() As Long
            Get
                Return Environment.WorkingSet
            End Get
        End Property


        '= PRIVATE ============================================================

        '''**************************************************************************
        ''' ;GetAttribute
        ''' <summary>
        ''' Get an attribute from the assembly and throw exception if the attribute does not exist.
        ''' </summary>
        ''' <param name="AttributeType">The type of the required attribute.</param>
        ''' <returns>The attribute with the given type gotten from the assembly, or Nothing.</returns>
        Private Function GetAttribute(ByVal AttributeType As Type) As Object

            Debug.Assert(m_Assembly IsNot Nothing, "Null m_Assembly")

            Dim Attributes() As Object = m_Assembly.GetCustomAttributes(AttributeType, inherit:=True)

            If Attributes.Length = 0 Then
                Return Nothing
            Else
                Return Attributes(0)
            End If
        End Function

        ' Private fields.
        Private m_Assembly As Assembly ' The assembly with the information.

        ' Since these properties will not change during run time, they're cached.
        ' "" is not Nothing so use Nothing to mark an un-accessed property.
        Private m_Description As String = Nothing ' Cache the assembly's description.
        Private m_Title As String = Nothing ' Cache the assembly's title.
        Private m_ProductName As String = Nothing ' Cache the assembly's product name.
        Private m_CompanyName As String = Nothing ' Cache the assembly's company name.
        Private m_Trademark As String = Nothing ' Cache the assembly's trademark.
        Private m_Copyright As String = Nothing ' Cache the assembly's copyright.
    End Class
End Namespace
