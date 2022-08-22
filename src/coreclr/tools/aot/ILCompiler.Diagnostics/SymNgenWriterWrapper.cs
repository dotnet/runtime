
using System;
using System.Runtime.InteropServices;
using System.Text;

#nullable enable

namespace Microsoft.DiaSymReader
{
    internal class SymNgenWriterWrapper : ISymNGenWriter2, IDisposable
    {
        private bool _isDisposed = false;
        public IntPtr ISymNGenWriter2Inst { get; }

        private SymNgenWriterWrapper(IntPtr writer2Inst)
        {
            ISymNGenWriter2Inst = writer2Inst;
        }

        public static SymNgenWriterWrapper? CreateIfSupported(IntPtr ptr)
        {
            var iid = ISymNGenWriter2.IID;
            int hr = Marshal.QueryInterface(ptr, ref iid, out IntPtr ngenWriterInst);
            Marshal.Release(ptr);
            if (hr != 0)
            {
                return null;
            }

            return new SymNgenWriterWrapper(ngenWriterInst);
        }

        ~SymNgenWriterWrapper()
        {
            DisposeInternal();
        }

        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        private void DisposeInternal()
        {
            if (_isDisposed)
            {
                return;
            }
            Marshal.Release(ISymNGenWriter2Inst);
            _isDisposed = true;
        }

        public unsafe void AddSymbol(string pSymbol, ushort iSection, ulong rva)
        {
            IntPtr strLocal = Marshal.StringToBSTR(pSymbol);
            var inst = ISymNGenWriter2Inst;
            var func = (delegate* unmanaged<IntPtr, IntPtr, ushort, ulong, int>)(*(*(void***)inst + 3 /* ISymNGenWriter2.AddSymbol slot */));
            int hr = func(inst, strLocal, iSection, rva);
            Marshal.FreeBSTR(strLocal);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public unsafe void AddSection(ushort iSection, OMF flags, int offset, int cb)
        {
            var inst = ISymNGenWriter2Inst;
            var func = (delegate* unmanaged<IntPtr, ushort, OMF, int, int, int>)(*(*(void***)inst + 4));
            int hr = func(inst, iSection, flags, offset, cb);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public unsafe void OpenModW(string wszModule, string wszObjFile, out UIntPtr ppmod)
        {
            var inst = ISymNGenWriter2Inst;
            fixed (char* wszModulePtr = wszModule)
            fixed (char* wszObjFilePtr = wszObjFile)
            {
                UIntPtr ppmodPtr;
                var func = (delegate* unmanaged<IntPtr, char*, char*, UIntPtr*, int>)(*(*(void***)inst + 5));
                int hr = func(inst, wszModulePtr, wszObjFilePtr, &ppmodPtr);
                ppmod = ppmodPtr;
                if (hr != 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }

        public unsafe void CloseMod(UIntPtr pmod)
        {
            var inst = ISymNGenWriter2Inst;
            var func = (delegate* unmanaged<IntPtr, UIntPtr, int>)(*(*(void***)inst + 6));
            int hr = func(inst, pmod);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public unsafe void ModAddSymbols(UIntPtr pmod, byte[] pbSym, int cb)
        {
            fixed (byte* pbSymPtr = pbSym)
            {
                var pbSymLocal = (IntPtr)pbSymPtr;
                var inst = ISymNGenWriter2Inst;
                var func = (delegate* unmanaged<IntPtr, UIntPtr, IntPtr, int, int>)(*(*(void***)inst + 7));
                int hr = func(inst, pmod, pbSymLocal, cb);
                if (hr != 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }

        public unsafe void ModAddSecContribEx(UIntPtr pmod, ushort isect, int off, int cb, uint dwCharacteristics, uint dwDataCrc, uint dwRelocCrc)
        {
            var inst = ISymNGenWriter2Inst;
            var func = (delegate* unmanaged<IntPtr, UIntPtr, ushort, int, int, uint, uint, uint, int>)(*(*(void***)inst + 8));
            int hr = func(inst, pmod, isect, off, cb, dwCharacteristics, dwDataCrc, dwRelocCrc);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public unsafe void QueryPDBNameExW(char[] pdb, IntPtr cchMax)
        {
            fixed (char* pdbPtr = pdb)
            {
                var pdbLocal = (IntPtr)pdbPtr;
                var inst = ISymNGenWriter2Inst;
                var func = (delegate* unmanaged<IntPtr, char*, IntPtr, int>)(*(*(void***)inst + 9));
                int hr = func(inst, pdbPtr, cchMax);
                if (hr != 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }

        void ISymNGenWriter.AddSymbol(string pSymbol, ushort iSection, ulong rva) => AddSymbol(pSymbol, iSection, rva);
        void ISymNGenWriter.AddSection(ushort iSection, OMF flags, int offset, int cb) => AddSection(iSection, flags, offset, cb);
    }
}
