// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.SymbolStore;

namespace System.Reflection.Emit
{
    //-----------------------------------------------------------------------------------
    // On Telesto, we don't ship the ISymWrapper.dll assembly. However, ReflectionEmit
    // relies on that assembly to write out managed PDBs.
    //
    // This file implements the minimum subset of ISymWrapper.dll required to restore
    // that functionality. Namely, the SymWriter and SymDocumentWriter objects.
    //
    // Ideally we wouldn't need ISymWrapper.dll on .NET Framework either - it's an ugly piece
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
    internal sealed class SymWrapperCore
    {
        //------------------------------------------------------------------------------
        // Block instantiation
        //------------------------------------------------------------------------------
        private SymWrapperCore()
        {
        }

        //------------------------------------------------------------------------------
        // Implements Telesto's version of SymDocumentWriter (in the .NET Framework world,
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
            internal SymDocumentWriter(PunkSafeHandle pDocumentWriterSafeHandle)
            {
                m_pDocumentWriterSafeHandle = pDocumentWriterSafeHandle;
                // The handle is actually a pointer to a native ISymUnmanagedDocumentWriter.
                m_pDocWriter = (ISymUnmanagedDocumentWriter*)m_pDocumentWriterSafeHandle.DangerousGetHandle();
                m_vtable = (ISymUnmanagedDocumentWriterVTable)(Marshal.PtrToStructure(m_pDocWriter->m_unmanagedVTable, typeof(ISymUnmanagedDocumentWriterVTable)))!;
            }

            //------------------------------------------------------------------------------
            // Returns the underlying ISymUnmanagedDocumentWriter* (as a safehandle.)
            //------------------------------------------------------------------------------
            internal PunkSafeHandle GetUnmanaged()
            {
                return m_pDocumentWriterSafeHandle;
            }


            // =========================================================================================
            // Public interface methods start here. (Well actually, they're all NotSupported
            // stubs since that's what they are on the real ISymWrapper.dll.)
            // =========================================================================================

            //------------------------------------------------------------------------------
            // SetSource() wrapper
            //------------------------------------------------------------------------------
            void ISymbolDocumentWriter.SetSource(byte[] source)
            {
                throw new NotSupportedException();   // Intentionally not supported to match .NET Framework
            }

            //------------------------------------------------------------------------------
            // SetCheckSum() wrapper
            //------------------------------------------------------------------------------
            void ISymbolDocumentWriter.SetCheckSum(Guid algorithmId, byte[] checkSum)
            {
                int hr = m_vtable.SetCheckSum(m_pDocWriter, algorithmId, (uint)checkSum.Length, checkSum);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr)!;
                }
            }

            private delegate int DSetCheckSum(ISymUnmanagedDocumentWriter* pThis, Guid algorithmId, uint checkSumSize, [In] byte[] checkSum);

            //------------------------------------------------------------------------------
            // This layout must match the unmanaged ISymUnmanagedDocumentWriter* COM vtable
            // exactly. If a member is declared as an IntPtr rather than a delegate, it means
            // we don't call that particular member.
            //------------------------------------------------------------------------------
            [StructLayout(LayoutKind.Sequential)]
            private struct ISymUnmanagedDocumentWriterVTable
            {
                internal IntPtr QueryInterface;
                internal IntPtr AddRef;
                internal IntPtr Release;

                internal IntPtr SetSource;
                internal DSetCheckSum SetCheckSum;
            }

            //------------------------------------------------------------------------------
            // This layout must match the (start) of the unmanaged ISymUnmanagedDocumentWriter
            // COM object.
            //------------------------------------------------------------------------------
            [StructLayout(LayoutKind.Sequential)]
            private struct ISymUnmanagedDocumentWriter
            {
                internal IntPtr m_unmanagedVTable;
            }

            //------------------------------------------------------------------------------
            // Stores underlying ISymUnmanagedDocumentWriter* pointer (wrapped in a safehandle.)
            //------------------------------------------------------------------------------
            private PunkSafeHandle m_pDocumentWriterSafeHandle;

            private ISymUnmanagedDocumentWriter* m_pDocWriter;

            //------------------------------------------------------------------------------
            // Stores the "managed vtable" (actually a structure full of delegates that
            // P/Invoke to the corresponding unmanaged COM methods.)
            //------------------------------------------------------------------------------
            private ISymUnmanagedDocumentWriterVTable m_vtable;
        } // class SymDocumentWriter


        //------------------------------------------------------------------------------
        // Implements Telesto's version of SymWriter (in the .NET Framework world,
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
            // but to fit in with existing .NET Framework code, the unmanaged writer is passed in
            // through a subsequent call to InternalSetUnderlyingWriter
            //------------------------------------------------------------------------------
            private SymWriter()
            {
            }

            //------------------------------------------------------------------------------
            // DefineDocument() wrapper
            //------------------------------------------------------------------------------
            ISymbolDocumentWriter? ISymbolWriter.DefineDocument(string url,
                                                               Guid language,
                                                               Guid languageVendor,
                                                               Guid documentType)
            {
                int hr = m_vtable.DefineDocument(m_pWriter, url, ref language, ref languageVendor, ref documentType, out PunkSafeHandle psymUnmanagedDocumentWriter);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr)!;
                }
                if (psymUnmanagedDocumentWriter.IsInvalid)
                {
                    return null;
                }
                return new SymDocumentWriter(psymUnmanagedDocumentWriter);
            }

            //------------------------------------------------------------------------------
            // OpenMethod() wrapper
            //------------------------------------------------------------------------------
            void ISymbolWriter.OpenMethod(SymbolToken method)
            {
                int hr = m_vtable.OpenMethod(m_pWriter, method.GetToken());
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr)!;
                }
            }

            //------------------------------------------------------------------------------
            // CloseMethod() wrapper
            //------------------------------------------------------------------------------
            void ISymbolWriter.CloseMethod()
            {
                int hr = m_vtable.CloseMethod(m_pWriter);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr)!;
                }
            }

            //------------------------------------------------------------------------------
            // DefineSequencePoints() wrapper
            //------------------------------------------------------------------------------
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
                if ((offsets != null && offsets.Length != spCount) ||
                     (lines != null && lines.Length != spCount) ||
                     (columns != null && columns.Length != spCount) ||
                     (endLines != null && endLines.Length != spCount) ||
                     (endColumns != null && endColumns.Length != spCount))
                {
                    throw new ArgumentException();
                }

                // Sure, claim to accept any type that implements ISymbolDocumentWriter but the only one that actually
                // works is the one returned by DefineDocument. The .NET Framework ISymWrapper commits the same signature fraud.
                // Ideally we'd just return a sealed opaque cookie type, which had an internal accessor to
                // get the writer out.
                // Regardless, this cast is important for security - we cannot allow our caller to provide
                // arbitrary instances of this interface.
                SymDocumentWriter docwriter = (SymDocumentWriter)document;
                int hr = m_vtable.DefineSequencePoints(m_pWriter, docwriter.GetUnmanaged(), spCount!, offsets!, lines!, columns!, endLines!, endColumns!);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr)!;
                }
            }

            //------------------------------------------------------------------------------
            // OpenScope() wrapper
            //------------------------------------------------------------------------------
            int ISymbolWriter.OpenScope(int startOffset)
            {
                int hr = m_vtable.OpenScope(m_pWriter, startOffset, out int ret);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr)!;
                }
                return ret;
            }

            //------------------------------------------------------------------------------
            // CloseScope() wrapper
            //------------------------------------------------------------------------------
            void ISymbolWriter.CloseScope(int endOffset)
            {
                int hr = m_vtable.CloseScope(m_pWriter, endOffset);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr)!;
                }
            }

            //------------------------------------------------------------------------------
            // DefineLocalVariable() wrapper
            //------------------------------------------------------------------------------
            void ISymbolWriter.DefineLocalVariable(string name,
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
                    throw Marshal.GetExceptionForHR(hr)!;
                }
            }

            //------------------------------------------------------------------------------
            // SetSymAttribute() wrapper
            //------------------------------------------------------------------------------
            void ISymbolWriter.SetSymAttribute(SymbolToken parent, string name, byte[] data)
            {
                int hr = m_vtable.SetSymAttribute(m_pWriter, parent.GetToken(), name, data.Length, data);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr)!;
                }
            }

            //------------------------------------------------------------------------------
            // UsingNamespace() wrapper
            //------------------------------------------------------------------------------
            void ISymbolWriter.UsingNamespace(string name)
            {
                int hr = m_vtable.UsingNamespace(m_pWriter, name);
                if (hr < 0)
                {
                    throw Marshal.GetExceptionForHR(hr)!;
                }
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
            internal void InternalSetUnderlyingWriter(IntPtr ppUnderlyingWriter)
            {
                m_pWriter = *((ISymUnmanagedWriter**)ppUnderlyingWriter);
                m_vtable = (ISymUnmanagedWriterVTable)(Marshal.PtrToStructure(m_pWriter->m_unmanagedVTable, typeof(ISymUnmanagedWriterVTable)))!;
            }

            //------------------------------------------------------------------------------
            // Define delegates for the unmanaged COM methods we invoke.
            //------------------------------------------------------------------------------
            private delegate int DInitialize(ISymUnmanagedWriter* pthis,
                                             IntPtr emitter,  // IUnknown*
                                             [MarshalAs(UnmanagedType.LPWStr)] string filename, // WCHAR*
                                             IntPtr pIStream, // IStream*
                                             [MarshalAs(UnmanagedType.Bool)] bool fFullBuild
                                             );

            private delegate int DDefineDocument(ISymUnmanagedWriter* pthis,
                                                 [MarshalAs(UnmanagedType.LPWStr)] string url,
                                                 [In] ref Guid language,
                                                 [In] ref Guid languageVender,
                                                 [In] ref Guid documentType,
                                                 [Out] out PunkSafeHandle ppsymUnmanagedDocumentWriter
                                                );

            private delegate int DSetUserEntryPoint(ISymUnmanagedWriter* pthis, int entryMethod);
            private delegate int DOpenMethod(ISymUnmanagedWriter* pthis, int entryMethod);
            private delegate int DCloseMethod(ISymUnmanagedWriter* pthis);

            private delegate int DDefineSequencePoints(ISymUnmanagedWriter* pthis,
                                                       PunkSafeHandle document,
                                                       int spCount,
                                                       [In] int[] offsets,
                                                       [In] int[] lines,
                                                       [In] int[] columns,
                                                       [In] int[] endLines,
                                                       [In] int[] endColumns);

            private delegate int DOpenScope(ISymUnmanagedWriter* pthis, int startOffset, [Out] out int pretval);
            private delegate int DCloseScope(ISymUnmanagedWriter* pthis, int endOffset);

            private delegate int DSetScopeRange(ISymUnmanagedWriter* pthis, int scopeID, int startOffset, int endOffset);

            private delegate int DDefineLocalVariable(ISymUnmanagedWriter* pthis,
                                                      [MarshalAs(UnmanagedType.LPWStr)] string name,
                                                      int attributes,
                                                      int cSig,
                                                      [In] byte[] signature,
                                                      int addrKind,
                                                      int addr1,
                                                      int addr2,
                                                      int addr3,
                                                      int startOffset,
                                                      int endOffset
                                                     );

            private delegate int DClose(ISymUnmanagedWriter* pthis);

            private delegate int DSetSymAttribute(ISymUnmanagedWriter* pthis,
                                                  int parent,
                                                  [MarshalAs(UnmanagedType.LPWStr)] string name,
                                                  int cData,
                                                  [In] byte[] data
                                                 );


            private delegate int DOpenNamespace(ISymUnmanagedWriter* pthis, [MarshalAs(UnmanagedType.LPWStr)] string name);
            private delegate int DCloseNamespace(ISymUnmanagedWriter* pthis);
            private delegate int DUsingNamespace(ISymUnmanagedWriter* pthis, [MarshalAs(UnmanagedType.LPWStr)] string name);



            //------------------------------------------------------------------------------
            // This layout must match the unmanaged ISymUnmanagedWriter* COM vtable
            // exactly. If a member is declared as an IntPtr rather than a delegate, it means
            // we don't call that particular member.
            //------------------------------------------------------------------------------
            [StructLayout(LayoutKind.Sequential)]
            private struct ISymUnmanagedWriterVTable
            {
                internal IntPtr QueryInterface;
                internal IntPtr AddRef;
                internal IntPtr Release;

                internal DDefineDocument DefineDocument;
                internal DSetUserEntryPoint SetUserEntryPoint;

                internal DOpenMethod OpenMethod;
                internal DCloseMethod CloseMethod;

                internal DOpenScope OpenScope;
                internal DCloseScope CloseScope;

                internal DSetScopeRange SetScopeRange;

                internal DDefineLocalVariable DefineLocalVariable;
                internal IntPtr DefineParameter;
                internal IntPtr DefineField;
                internal IntPtr DefineGlobalVariable;

                internal DClose Close;
                internal DSetSymAttribute SetSymAttribute;

                internal DOpenNamespace OpenNamespace;
                internal DCloseNamespace CloseNamespace;
                internal DUsingNamespace UsingNamespace;

                internal IntPtr SetMethodSourceRange;
                internal DInitialize Initialize;
                internal IntPtr GetDebugInfo;
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
            private ISymUnmanagedWriter* m_pWriter;

            //------------------------------------------------------------------------------
            // Stores the "managed vtable" (actually a structure full of delegates that
            // P/Invoke to the corresponding unmanaged COM methods.)
            //------------------------------------------------------------------------------
            private ISymUnmanagedWriterVTable m_vtable;
        } // class SymWriter
    } // class SymWrapperCore



    //--------------------------------------------------------------------------------------
    // SafeHandle for RAW MTA IUnknown's.
    //
    // ! Because the Release occurs in the finalizer thread, this safehandle really takes
    // ! an ostrich approach to apartment issues. We only tolerate this here because we're emulating
    // ! the .NET Framework's use of ISymWrapper which also pays lip service to COM apartment rules.
    // !
    // ! However, think twice about pulling this safehandle out for other uses.
    //
    // Had to make this a non-nested class since FCall's don't like to bind to nested classes.
    //--------------------------------------------------------------------------------------
    internal sealed class PunkSafeHandle : SafeHandle
    {
        public PunkSafeHandle()
            : base((IntPtr)0, true)
        {
        }

        protected override bool ReleaseHandle()
        {
            m_Release(handle);
            return true;
        }

        public override bool IsInvalid => handle == ((IntPtr)0);

        private delegate void DRelease(IntPtr punk);         // Delegate type for P/Invoking to coreclr.dll and doing an IUnknown::Release()
        private static DRelease m_Release = (DRelease)Marshal.GetDelegateForFunctionPointer(nGetDReleaseTarget(), typeof(DRelease));

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr nGetDReleaseTarget();     // FCall gets us the native DRelease target (so we don't need named dllexport from coreclr.dll)

        static PunkSafeHandle()
        {
            m_Release((IntPtr)0); // make one call to make sure the delegate is fully prepped before we're in the critical finalizer situation.
        }
    } // PunkSafeHandle
} // namespace System.Reflection.Emit
