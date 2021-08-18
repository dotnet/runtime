/**
 * \file
 * This file contains the default set of the mono qcalls.
 * Each type that has qcall methods must be declared here
 * with the FCClassElement macro as follows:
 *
 * 	FCClassElement(class_name, namespace, symbol_name)
 *  where symbol_name is an array of MonoQCallFunc.
 *
 * FCClassElements have to be sorted by name then namespace, 
 * but the functions in each one can be in any order, 
 * specified by the QCFuncElement macro as follows:
 *      FCFuncStart(class_symbol_name)
 *            QCFuncElement(method_name, symbol_name)
 *      FCFuncEnd()
 * where class_symbol_name is the symbol for the corresponding
 * class specified in FCClassElement, method_name is the name of the
 * method on the managed side, and symbol_name is the name
 * of the implementing method on the native side.
 **/

FCFuncStart(gInteropNativeLibraryFuncs)
	QCFuncElement("GetEntryPointModuleHandleInternal", netcore_lookup_self_native_handle)
FCFuncEnd()

FCClassElement("NativeLibrary", "System.Runtime.InteropServices", gInteropNativeLibraryFuncs)
