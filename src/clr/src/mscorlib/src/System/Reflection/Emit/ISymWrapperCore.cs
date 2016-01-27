// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

#if FEATURE_CORECLR

namespace System.Reflection.Emit
{
    using System;
    using System.Security;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.SymbolStore;


    //-----------------------------------------------------------------------------------
    // On Telesto, we don't ship the ISymWrapper.dll assembly. However, ReflectionEmit
    // relies on that assembly to write out managed PDBs.
    //
    // This file implements the minimum subset of ISymWrapper.dll required to restore
    // that functionality. Namely, the SymWriter and SymDocumentWriter objects.
    // 
    // Ideally we wouldn't need ISymWrapper.dll on desktop either - it's an ugly piece
    // of legacy.  We could just use this (or COM-interop code) everywhere, but we might
    // have to worry about compatibility.
    // 
    // We've now got a real implementation even when no debugger is attached.  It's
    // up to the runtime to ensure it doesn't provide us with an insecure writer 
    // (eg. diasymreader) in the no-trust scenarios (no debugger, partial-trust code).
    //-----------------------------------------------------------------------------------


    //------------------------------------------------------------------------------
    // SymWrapperCore is never instantiated and is used as an encapsulation class.
    // It is our "ISymWrapper.dll" assembly within an assembly.
    //------------------------------------------------------------------------------
    class SymWrapperCore
    {
        //------------------------------------------------------------------------------
        // Block instantiation
        //------------------------------------------------------------------------------
        private SymWrapperCore()
        {
        }

        //------------------------------------------------------------------------------
        // Implements Telesto's version of SymDocumentWriter (in the desktop world,
        // this type is exposed from ISymWrapper.dll.)
        //
        // The only thing user code can do with this wrapper is to receive it from
        // SymWriter.DefineDocument and pass it back to SymWriter.DefineSequencePoints.
        //------------------------------------------------------------------------------
        private unsafe class SymDocumentWriter : ISymbolDocumentWriter
        {
    
            //------------------------------------------------------------------------------
            // Ctor
            //------------------------------------------------------------------------------
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            internal SymDocumentWriter(PunkSafeHandle pDocumentWriterSafeHandle)
            {
                m_pDocumentWriterSafeHandle = pDocumentWriterSafeHandle;
                // The handle is actually a pointer to a native ISymUnmanagedDocumentWriter.
                m_pDocWriter = (ISymUnmanagedDocumentWriter *)m_pDocumentWriterSafeHandle.DangerousGetHandle();
                m_vtable = (ISymUnmanagedDocumentWriterVTable)(Marshal.PtrToStructure(m_pDocWriter->m_unmanagedVTable, typeof(ISymUnmanagedDocumentWriterVTable)));
            }
    
            //------------------------------------------------------------------------------
            // Returns the underlying ISymUnmanagedDocumentWriter* (as a safehandle.)
            //------------------------------------------------------------------------------
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            internal PunkSafeHandle GetUnmanaged()
            {
                return m_pDocumentWriterSafeHandle;
            }
    

            //=========================================================================================
            // Public interface methods start here. (Well actually, they're all NotSupported
            // stubs since that's what they are on the real ISymWrapper.dll.)
            //=========================================================================================

            //------------------------------------------------------------------------------
            // SetSource() wrapper
            //------------------------------------------------------------------------------
            void ISymbolDocumentWriter.SetSource(byte[] source)
            {
                throw new NotSupportedException();   // Intentionally not supported to match desktop CLR
            }
    
            //------------------------------------------------------------------------------
            // SetCheckSum() wrapper
            //------------------------------------------------------------------------------
            #if FEATURE_CORECLR
            [System.Security.SecuritySafeCritical]
            #endif
            void ISymbolDocumentWriter.SetCheckSum(Guid algorithmId, byte [] checkSum)
            {
                int hr = m_vtable.SetCheckSum(m_pDocWriter, algorithmId, (uint)checkSum.Length, checkSum);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }
    
            [System.Security.SecurityCritical]
            private delegate int DSetCheckSum(ISymUnmanagedDocumentWriter * pThis, Guid algorithmId, uint checkSumSize, [In] byte[] checkSum);

            //------------------------------------------------------------------------------
            // This layout must match the unmanaged ISymUnmanagedDocumentWriter* COM vtable
            // exactly. If a member is declared as an IntPtr rather than a delegate, it means
            // we don't call that particular member.
            //------------------------------------------------------------------------------
            [System.Security.SecurityCritical]
            [StructLayout(LayoutKind.Sequential)]
            private struct ISymUnmanagedDocumentWriterVTable
            {
                internal IntPtr                QueryInterface;
                internal IntPtr                AddRef;
                internal IntPtr                Release;

                internal IntPtr                SetSource;
                #if FEATURE_CORECLR
                [System.Security.SecurityCritical]
                #endif
                internal DSetCheckSum          SetCheckSum;
            }
    
            //------------------------------------------------------------------------------
            // This layout must match the (start) of the unmanaged ISymUnmanagedDocumentWriter
            // COM object.
            //------------------------------------------------------------------------------
            [System.Security.SecurityCritical]
            [StructLayout(LayoutKind.Sequential)]
            private struct ISymUnmanagedDocumentWriter
            {
                internal IntPtr m_unmanagedVTable;
            }

            //------------------------------------------------------------------------------
            // Stores underlying ISymUnmanagedDocumentWriter* pointer (wrapped in a safehandle.)
            //------------------------------------------------------------------------------
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            private PunkSafeHandle m_pDocumentWriterSafeHandle;

            [SecurityCritical]
            private ISymUnmanagedDocumentWriter * m_pDocWriter;

            //------------------------------------------------------------------------------
            // Stores the "managed vtable" (actually a structure full of delegates that
            // P/Invoke to the corresponding unmanaged COM methods.)
            //------------------------------------------------------------------------------
            [SecurityCritical]
            private ISymUnmanagedDocumentWriterVTable m_vtable;

    
        } // class SymDocumentWriter


        //------------------------------------------------------------------------------
        // Implements Telesto's version of SymWriter (in the desktop world,
        // this type is expored from ISymWrapper.dll.)
        //------------------------------------------------------------------------------
        internal unsafe class SymWriter : ISymbolWriter
        {


            //------------------------------------------------------------------------------
            // Creates a SymWriter. The SymWriter is a managed wrapper around the unmanaged 
            // symbol writer provided by the runtime (ildbsymlib or diasymreader.dll).
            //------------------------------------------------------------------------------
            internal static ISymbolWriter CreateSymWriter()
            {
                return new SymWriter();
            }


            //------------------------------------------------------------------------------
            // Basic ctor. You'd think this ctor would take the unmanaged symwriter object as an argument
            // but to fit in with existing desktop code, the unmanaged writer is passed in 
            // through a subsequent call to InternalSetUnderlyingWriter
            //------------------------------------------------------------------------------
            private SymWriter()
            {
            }

            //=========================================================================================
            // Public interface methods start here.
            //=========================================================================================


            //------------------------------------------------------------------------------
            // Initialize() wrapper
            //------------------------------------------------------------------------------
            void ISymbolWriter.Initialize(IntPtr emitter, String filename, bool fFullBuild)
            {
                int hr = m_vtable.Initialize(m_pWriter, emitter, filename, (IntPtr)0, fFullBuild);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }
            
            //------------------------------------------------------------------------------
            // DefineDocument() wrapper
            //------------------------------------------------------------------------------
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            ISymbolDocumentWriter ISymbolWriter.DefineDocument(String url,
                                                               Guid language,
                                                               Guid languageVendor,
                                                               Guid documentType)
            {
                PunkSafeHandle psymUnmanagedDocumentWriter = new PunkSafeHandle();

                int hr = m_vtable.DefineDocument(m_pWriter, url, ref language, ref languageVendor, ref documentType, out psymUnmanagedDocumentWriter);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
                if (psymUnmanagedDocumentWriter.IsInvalid)
                {
                    return null;
                }
                return new SymDocumentWriter(psymUnmanagedDocumentWriter);
            }
        
            //------------------------------------------------------------------------------
            // SetUserEntryPoint() wrapper
            //------------------------------------------------------------------------------
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            void ISymbolWriter.SetUserEntryPoint(SymbolToken entryMethod)
            {
                int hr = m_vtable.SetUserEntryPoint(m_pWriter, entryMethod.GetToken());
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }
        
            //------------------------------------------------------------------------------
            // OpenMethod() wrapper
            //------------------------------------------------------------------------------
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            void ISymbolWriter.OpenMethod(SymbolToken method)
            {
                int hr = m_vtable.OpenMethod(m_pWriter, method.GetToken());
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }
        
            //------------------------------------------------------------------------------
            // CloseMethod() wrapper
            //------------------------------------------------------------------------------
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            void ISymbolWriter.CloseMethod()
            {
                int hr = m_vtable.CloseMethod(m_pWriter);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }
        
            //------------------------------------------------------------------------------
            // DefineSequencePoints() wrapper
            //------------------------------------------------------------------------------
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            void ISymbolWriter.DefineSequencePoints(ISymbolDocumentWriter document,
                                                    int[] offsets,
                                                    int[] lines,
                                                    int[] columns,
                                                    int[] endLines,
                                                    int[] endColumns)
            {
                int spCount = 0;
                if (offsets != null)
                {
                    spCount = offsets.Length;
                }
                else if (lines != null)
                {
                    spCount = lines.Length;
                }
                else if (columns != null)
                {
                    spCount = columns.Length;
                }
                else if (endLines != null)
                {
                    spCount = endLines.Length;
                }
                else if (endColumns != null)
                {
                    spCount = endColumns.Length;
                }
                if (spCount == 0)
                {
                    return;
                }
                if ( (offsets != null && offsets.Length != spCount) ||
                     (lines != null && lines.Length != spCount)     ||
                     (columns != null && columns.Length != spCount) ||
                     (endLines != null && endLines.Length != spCount) ||
                     (endColumns != null && endColumns.Length != spCount) )
                {
                    throw new ArgumentException();
                }

                // Sure, claim to accept any type that implements ISymbolDocumentWriter but the only one that actually
                // works is the one returned by DefineDocument. The desktop ISymWrapper commits the same signature fraud.
                // Ideally we'd just return a sealed opaque cookie type, which had an internal accessor to
                // get the writer out.
                // Regardless, this cast is important for security - we cannot allow our caller to provide
                // arbitrary instances of this interface.
                SymDocumentWriter docwriter = (SymDocumentWriter)document;
                int hr = m_vtable.DefineSequencePoints(m_pWriter, docwriter.GetUnmanaged(), spCount, offsets, lines, columns, endLines, endColumns);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }

            }
        
            //------------------------------------------------------------------------------
            // OpenScope() wrapper
            //------------------------------------------------------------------------------
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            int ISymbolWriter.OpenScope(int startOffset)
            {
                int ret;
                int hr = m_vtable.OpenScope(m_pWriter, startOffset, out ret);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
                return ret;
            }
        
            //------------------------------------------------------------------------------
            // CloseScope() wrapper
            //------------------------------------------------------------------------------
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            void ISymbolWriter.CloseScope(int endOffset)
            {
                int hr = m_vtable.CloseScope(m_pWriter, endOffset);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }
        
            //------------------------------------------------------------------------------
            // SetScopeRange() wrapper
            //------------------------------------------------------------------------------
            void ISymbolWriter.SetScopeRange(int scopeID, int startOffset, int endOffset)
            {
                int hr = m_vtable.SetScopeRange(m_pWriter, scopeID, startOffset, endOffset);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }
    
            //------------------------------------------------------------------------------
            // DefineLocalVariable() wrapper
            //------------------------------------------------------------------------------
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            void ISymbolWriter.DefineLocalVariable(String name,
                                                   FieldAttributes attributes,
                                                   byte[] signature,
                                                   SymAddressKind addrKind,
                                                   int addr1,
                                                   int addr2,
                                                   int addr3,
                                                   int startOffset,
                                                   int endOffset)
            {
                int hr = m_vtable.DefineLocalVariable(m_pWriter,
                                                      name,
                                                      (int)attributes,
                                                      signature.Length,
                                                      signature,
                                                      (int)addrKind,
                                                      addr1,
                                                      addr2,
                                                      addr3,
                                                      startOffset,
                                                      endOffset);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }
        
            //------------------------------------------------------------------------------
            // DefineParameter() wrapper
            //------------------------------------------------------------------------------
            void ISymbolWriter.DefineParameter(String name,
                                               ParameterAttributes attributes,
                                               int sequence,
                                               SymAddressKind addrKind,
                                               int addr1,
                                               int addr2,
                                               int addr3)
            {
                throw new NotSupportedException();  // Intentionally not supported to match desktop CLR
            }
        
            //------------------------------------------------------------------------------
            // DefineField() wrapper
            //------------------------------------------------------------------------------
            void ISymbolWriter.DefineField(SymbolToken parent,
                                           String name,
                                           FieldAttributes attributes,
                                           byte[] signature,
                                           SymAddressKind addrKind,
                                           int addr1,
                                           int addr2,
                                           int addr3)
            {
                throw new NotSupportedException();  // Intentionally not supported to match desktop CLR
            }
        
            //------------------------------------------------------------------------------
            // DefineGlobalVariable() wrapper
            //------------------------------------------------------------------------------
            void ISymbolWriter.DefineGlobalVariable(String name,
                                                    FieldAttributes attributes,
                                                    byte[] signature,
                                                    SymAddressKind addrKind,
                                                    int addr1,
                                                    int addr2,
                                                    int addr3)
            {
                throw new NotSupportedException();  // Intentionally not supported to match desktop CLR
            }
        
            //------------------------------------------------------------------------------
            // Close() wrapper
            //------------------------------------------------------------------------------
            void ISymbolWriter.Close()
            {
                int hr = m_vtable.Close(m_pWriter);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }
        
            //------------------------------------------------------------------------------
            // SetSymAttribute() wrapper
            //------------------------------------------------------------------------------
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            void ISymbolWriter.SetSymAttribute(SymbolToken parent, String name, byte[] data)
            {
                int hr = m_vtable.SetSymAttribute(m_pWriter, parent.GetToken(), name, data.Length, data);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }
        
            //------------------------------------------------------------------------------
            // OpenNamespace() wrapper
            //------------------------------------------------------------------------------
            void ISymbolWriter.OpenNamespace(String name)
            {
                int hr = m_vtable.OpenNamespace(m_pWriter, name);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }
        
            //------------------------------------------------------------------------------
            // CloseNamespace() wrapper
            //------------------------------------------------------------------------------
            void ISymbolWriter.CloseNamespace()
            {
                int hr = m_vtable.CloseNamespace(m_pWriter);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }
        
            //------------------------------------------------------------------------------
            // UsingNamespace() wrapper
            //------------------------------------------------------------------------------
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            void ISymbolWriter.UsingNamespace(String name)
            {
                int hr = m_vtable.UsingNamespace(m_pWriter, name);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }
            
            //------------------------------------------------------------------------------
            // SetMethodSourceRange() wrapper
            //------------------------------------------------------------------------------
            void ISymbolWriter.SetMethodSourceRange(ISymbolDocumentWriter startDoc,
                                                    int startLine,
                                                    int startColumn,
                                                    ISymbolDocumentWriter endDoc,
                                                    int endLine,
                                                    int endColumn)
            {
                throw new NotSupportedException();   // Intentionally not supported to match desktop CLR
            }
    
            //------------------------------------------------------------------------------
            // SetUnderlyingWriter() wrapper.
            //------------------------------------------------------------------------------
            void ISymbolWriter.SetUnderlyingWriter(IntPtr ppUnderlyingWriter)
            {
                throw new NotSupportedException();   // Intentionally not supported on Telesto as it's a very unsafe api
            }

            //------------------------------------------------------------------------------
            // InternalSetUnderlyingWriter() wrapper.
            //
            // Furnishes the native ISymUnmanagedWriter* pointer. 
            //
            // The parameter is actually a pointer to a pointer to an ISymUnmanagedWriter. As
            // with the real ISymWrapper.dll, ISymWrapper performs *no* Release (or AddRef) on pointers
            // furnished through SetUnderlyingWriter. Lifetime management is entirely up to the caller.
            //------------------------------------------------------------------------------
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            internal void InternalSetUnderlyingWriter(IntPtr ppUnderlyingWriter)
            {
                m_pWriter = *((ISymUnmanagedWriter**)ppUnderlyingWriter);
                m_vtable  = (ISymUnmanagedWriterVTable) (Marshal.PtrToStructure(m_pWriter->m_unmanagedVTable, typeof(ISymUnmanagedWriterVTable)));
            }

            //------------------------------------------------------------------------------
            // Define delegates for the unmanaged COM methods we invoke.
            //------------------------------------------------------------------------------
            [System.Security.SecurityCritical]
            private delegate int DInitialize(ISymUnmanagedWriter*                     pthis,
                                             IntPtr                                   emitter,  //IUnknown*
                                             [MarshalAs(UnmanagedType.LPWStr)] String filename, //WCHAR*
                                             IntPtr                                   pIStream, //IStream*
                                             [MarshalAs(UnmanagedType.Bool)] bool     fFullBuild
                                             );

            [System.Security.SecurityCritical]
            private delegate int DDefineDocument(ISymUnmanagedWriter*                     pthis,
                                                 [MarshalAs(UnmanagedType.LPWStr)] String url,
                                                 [In] ref Guid                            language,
                                                 [In] ref Guid                            languageVender,
                                                 [In] ref Guid                            documentType,
                                                 [Out] out PunkSafeHandle                 ppsymUnmanagedDocumentWriter
                                                );
                                                                              
            [System.Security.SecurityCritical]
            private delegate int DSetUserEntryPoint(ISymUnmanagedWriter* pthis, int entryMethod);
            [System.Security.SecurityCritical]
            private delegate int DOpenMethod(ISymUnmanagedWriter* pthis, int entryMethod);
            [System.Security.SecurityCritical]
            private delegate int DCloseMethod(ISymUnmanagedWriter* pthis);

            [System.Security.SecurityCritical]
            private delegate int DDefineSequencePoints(ISymUnmanagedWriter* pthis,
                                                       PunkSafeHandle       document,
                                                       int                  spCount,
                                                       [In] int[]           offsets,
                                                       [In] int[]           lines,
                                                       [In] int[]           columns,
                                                       [In] int[]           endLines,
                                                       [In] int[]           endColumns);

            [System.Security.SecurityCritical]
            private delegate int DOpenScope(ISymUnmanagedWriter* pthis, int startOffset, [Out] out int pretval);
            [System.Security.SecurityCritical]
            private delegate int DCloseScope(ISymUnmanagedWriter* pthis, int endOffset);

            [System.Security.SecurityCritical]
            private delegate int DSetScopeRange(ISymUnmanagedWriter* pthis, int scopeID, int startOffset, int endOffset);

            [System.Security.SecurityCritical]
            private delegate int DDefineLocalVariable(ISymUnmanagedWriter*                     pthis,
                                                      [MarshalAs(UnmanagedType.LPWStr)] String name,
                                                      int                                      attributes,
                                                      int                                      cSig,
                                                      [In] byte[]                              signature,
                                                      int                                      addrKind,
                                                      int                                      addr1,
                                                      int                                      addr2,
                                                      int                                      addr3,
                                                      int                                      startOffset,
                                                      int                                      endOffset
                                                     );

            [System.Security.SecurityCritical]
            private delegate int DClose(ISymUnmanagedWriter* pthis);

            [System.Security.SecurityCritical]
            private delegate int DSetSymAttribute(ISymUnmanagedWriter*                     pthis,
                                                  int                                      parent,
                                                  [MarshalAs(UnmanagedType.LPWStr)] String name,
                                                  int                                      cData,
                                                  [In] byte[]                              data
                                                 );


            [System.Security.SecurityCritical]
            private delegate int DOpenNamespace(ISymUnmanagedWriter* pthis, [MarshalAs(UnmanagedType.LPWStr)] String name);
            [System.Security.SecurityCritical]
            private delegate int DCloseNamespace(ISymUnmanagedWriter* pthis);
            [System.Security.SecurityCritical]
            private delegate int DUsingNamespace(ISymUnmanagedWriter* pthis, [MarshalAs(UnmanagedType.LPWStr)] String name);



            //------------------------------------------------------------------------------
            // This layout must match the unmanaged ISymUnmanagedWriter* COM vtable
            // exactly. If a member is declared as an IntPtr rather than a delegate, it means
            // we don't call that particular member.
            //------------------------------------------------------------------------------
            [StructLayout(LayoutKind.Sequential)]
            private struct ISymUnmanagedWriterVTable
            {
                internal IntPtr                QueryInterface;
                internal IntPtr                AddRef;
                internal IntPtr                Release;

                #if FEATURE_CORECLR
                [System.Security.SecurityCritical] // auto-generated
                #endif
                internal DDefineDocument       DefineDocument;
                #if FEATURE_CORECLR
                [System.Security.SecurityCritical] // auto-generated
                #endif
                internal DSetUserEntryPoint    SetUserEntryPoint;

                #if FEATURE_CORECLR
                [System.Security.SecurityCritical] // auto-generated
                #endif
                internal DOpenMethod           OpenMethod;
                #if FEATURE_CORECLR
                [System.Security.SecurityCritical] // auto-generated
                #endif
                internal DCloseMethod          CloseMethod;

                #if FEATURE_CORECLR
                [System.Security.SecurityCritical] // auto-generated
                #endif
                internal DOpenScope            OpenScope;
                #if FEATURE_CORECLR
                [System.Security.SecurityCritical] // auto-generated
                #endif
                internal DCloseScope           CloseScope;

                #if FEATURE_CORECLR
                [System.Security.SecurityCritical] // auto-generated
                #endif
                internal DSetScopeRange        SetScopeRange;

                #if FEATURE_CORECLR
                [System.Security.SecurityCritical] // auto-generated
                #endif
                internal DDefineLocalVariable  DefineLocalVariable;
                internal IntPtr                DefineParameter;
                internal IntPtr                DefineField;
                internal IntPtr                DefineGlobalVariable;

                #if FEATURE_CORECLR
                [System.Security.SecurityCritical] // auto-generated
                #endif
                internal DClose                Close;
                #if FEATURE_CORECLR
                [System.Security.SecurityCritical] // auto-generated
                #endif
                internal DSetSymAttribute      SetSymAttribute;

                #if FEATURE_CORECLR
                [System.Security.SecurityCritical] // auto-generated
                #endif
                internal DOpenNamespace        OpenNamespace;
                #if FEATURE_CORECLR
                [System.Security.SecurityCritical] // auto-generated
                #endif
                internal DCloseNamespace       CloseNamespace;
                #if FEATURE_CORECLR
                [System.Security.SecurityCritical] // auto-generated
                #endif
                internal DUsingNamespace       UsingNamespace;

                internal IntPtr                SetMethodSourceRange;
                #if FEATURE_CORECLR
                [System.Security.SecurityCritical] // auto-generated
                #endif
                internal DInitialize           Initialize;
                internal IntPtr                GetDebugInfo;
                #if FEATURE_CORECLR
                [System.Security.SecurityCritical] // auto-generated
                #endif
                internal DDefineSequencePoints DefineSequencePoints;
        
            }

            //------------------------------------------------------------------------------
            // This layout must match the (start) of the unmanaged ISymUnmanagedWriter
            // COM object.
            //------------------------------------------------------------------------------
            [StructLayout(LayoutKind.Sequential)]
            private struct ISymUnmanagedWriter
            {
                internal IntPtr m_unmanagedVTable;
            }

            //------------------------------------------------------------------------------
            // Stores native ISymUnmanagedWriter* pointer.
            //
            // As with the real ISymWrapper.dll, ISymWrapper performs *no* Release (or AddRef) on this pointer.
            // Managing lifetime is up to the caller (coreclr.dll).
            //------------------------------------------------------------------------------
            [SecurityCritical]
            private ISymUnmanagedWriter      *m_pWriter;

            //------------------------------------------------------------------------------
            // Stores the "managed vtable" (actually a structure full of delegates that
            // P/Invoke to the corresponding unmanaged COM methods.)
            //------------------------------------------------------------------------------
            private ISymUnmanagedWriterVTable m_vtable;

        } // class SymWriter




    } //class SymWrapperCore



    //--------------------------------------------------------------------------------------
    // SafeHandle for RAW MTA IUnknown's.
    //
    // ! Because the Release occurs in the finalizer thread, this safehandle really takes
    // ! an ostrich approach to apartment issues. We only tolerate this here because we're emulating
    // ! the desktop CLR's use of ISymWrapper which also pays lip service to COM apartment rules.
    // !
    // ! However, think twice about pulling this safehandle out for other uses.
    //
    // Had to make this a non-nested class since FCall's don't like to bind to nested classes.
    //--------------------------------------------------------------------------------------
    #if FEATURE_CORECLR
    [System.Security.SecurityCritical] // auto-generated
    #endif
    sealed class PunkSafeHandle : SafeHandle
    {
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal PunkSafeHandle()
            : base((IntPtr)0, true)
        {
        }

        [SecurityCritical]
        override protected bool ReleaseHandle()
        {
            m_Release(handle);
            return true;
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return handle == ((IntPtr)0); }
        }

        private delegate void DRelease(IntPtr punk);         // Delegate type for P/Invoking to coreclr.dll and doing an IUnknown::Release()
        private static DRelease m_Release;

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr nGetDReleaseTarget();     // FCall gets us the native DRelease target (so we don't need named dllexport from coreclr.dll)

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        static PunkSafeHandle()
        {
            m_Release = (DRelease)(Marshal.GetDelegateForFunctionPointer(nGetDReleaseTarget(), typeof(DRelease)));
            m_Release((IntPtr)0); // make one call to make sure the delegate is fully prepped before we're in the critical finalizer situation.
        }

    } // PunkSafeHandle

} //namespace System.Reflection.Emit


#endif //FEATURE_CORECLR

