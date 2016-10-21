#!/bin/bash

if test x$1 = x; then
   echo usage is cleanup MONODIR
   exit 1
fi

MONODIR=$1

cd $MONODIR
rm -rf lib/gtk-2.0/2.10.0/engines/libcrux-engine.so
rm -rf lib/gtk-2.0/2.10.0/engines/libglide.so
rm -rf lib/gtk-2.0/2.10.0/engines/libhcengine.so
rm -rf lib/gtk-2.0/2.10.0/engines/libindustrial.so
rm -rf lib/gtk-2.0/2.10.0/engines/libmist.so
rm -rf lib/gtk-2.0/2.10.0/engines/libpixmap.so
rm -rf lib/gtk-2.0/2.10.0/engines/libredmond95.so
rm -rf lib/gtk-2.0/2.10.0/engines/libthinice.so
rm -rf gtk-2.0/modules/libferret.*
rm -rf gtk-2.0/modules/libgail.*
rm -rf share/gtk-2.0/demo/*
rm -rf share/man/man1/oldmono.1
rm -rf share/themes/Crux
rm -rf share/themes/Default
rm -rf share/themes/Emacs
rm -rf share/themes/Industrial
rm -rf share/themes/Mist
rm -rf share/themes/Raleigh
rm -rf share/themes/Redmond
rm -rf share/themes/ThinIce
rm -rf share/info
rm -rf share/icons/gnome
rm -rf share/icons/hicolor
rm -rf share/gtk-doc
rm -rf share/gettext/*.class
rm -rf share/doc
rm -rf share/emacs
rm -rf share/strings
rm -rf share/pixmaps
rm -rf share/intltool
rm -rf var/cache/fontconfig

# delete most of the *.a files
rm -rf lib/cairo/libcairo-trace.a
rm -rf lib/gdk-pixbuf-2.0/2.10.0/loaders/libpixbufloader-svg.a
rm -rf lib/gtk-2.0/2.10.0/engines/libsvg.a
rm -rf lib/libCompilerDriver.a
rm -rf lib/libEnhancedDisassembly.a
rm -rf lib/libLLVMAnalysis.a
rm -rf lib/libLLVMArchive.a
rm -rf lib/libLLVMAsmParser.a
rm -rf lib/libLLVMAsmPrinter.a
rm -rf lib/libLLVMBitReader.a
rm -rf lib/libLLVMBitWriter.a
rm -rf lib/libLLVMCodeGen.a
rm -rf lib/libLLVMCore.a
rm -rf lib/libLLVMExecutionEngine.a
rm -rf lib/libLLVMInstCombine.a
rm -rf lib/libLLVMInstrumentation.a
rm -rf lib/libLLVMInterpreter.a
rm -rf lib/libLLVMJIT.a
rm -rf lib/libLLVMLinker.a
rm -rf lib/libLLVMMC.a
rm -rf lib/libLLVMMCDisassembler.a
rm -rf lib/libLLVMMCJIT.a
rm -rf lib/libLLVMMCParser.a
rm -rf lib/libLLVMObject.a
rm -rf lib/libLLVMScalarOpts.a
rm -rf lib/libLLVMSelectionDAG.a
rm -rf lib/libLLVMSupport.a
rm -rf lib/libLLVMTarget.a
rm -rf lib/libLLVMTransformUtils.a
rm -rf lib/libLLVMX86AsmParser.a
rm -rf lib/libLLVMX86AsmPrinter.a
rm -rf lib/libLLVMX86CodeGen.a
rm -rf lib/libLLVMX86Disassembler.a
rm -rf lib/libLLVMX86Info.a
rm -rf lib/libLLVMipa.a
rm -rf lib/libLLVMipo.a
rm -rf lib/libLTO.a
# rm -rf lib/libMonoPosixHelper.a
# rm -rf lib/libMonoSupportW.a
rm -rf lib/libUnitTestMain.a
rm -rf lib/libatksharpglue-2.a
rm -rf lib/libcairo-gobject.a
rm -rf lib/libcairo-script-interpreter.a
rm -rf lib/libcairo.a
rm -rf lib/libcroco-0.6.a
rm -rf lib/libexpat.a
rm -rf lib/libffi.a
rm -rf lib/libfontconfig.a
rm -rf lib/libfreetype.a
rm -rf lib/libgdiplus.a
rm -rf lib/libgdksharpglue-2.a
rm -rf lib/libgettextpo.a
rm -rf lib/libgif.a
rm -rf lib/libglade-2.0.a
rm -rf lib/libgladesharpglue-2.a
rm -rf lib/libglibsharpglue-2.a
rm -rf lib/libgtksharpglue-2.a
rm -rf lib/libikvm-native.a
rm -rf lib/libintl.a
rm -rf lib/libjpeg.a
rm -rf lib/liblzma.a
# rm -rf lib/libmono-2.0.a
# rm -rf lib/libmono-llvm.a
# rm -rf lib/libmono-profiler-aot.a
# rm -rf lib/libmono-profiler-cov.a
# rm -rf lib/libmono-profiler-iomap.a
# rm -rf lib/libmono-profiler-log.a
# rm -rf lib/libmonosgen-2.0.a
rm -rf lib/libpangosharpglue-2.a
rm -rf lib/libpixman-1.a
rm -rf lib/libpng.a
rm -rf lib/libpng14.a
rm -rf lib/librsvg-2.a
rm -rf lib/libsqlite3.a
rm -rf lib/libtiff.a
rm -rf lib/libtiffxx.a
rm -rf lib/libxml2.a

# we don't need any of the llvm executables except llc and opt
rm -rf bin/bugpoint
rm -rf bin/lli
rm -rf bin/llvm-*
rm -rf bin/macho-dump
rm -rf bin/ccache

#
# 14:39 <baulig> the install script needs to be modified not to
#                install .mdb's for these
# 14:39 <baulig> System.Windows.dll, System.Xml.Serialization.dll and
#                everything in Facades

find ./lib/mono/4.5/Facades -name "*.mdb" -delete
