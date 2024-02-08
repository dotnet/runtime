#!/usr/bin/env python3
# -*- Mode: python; tab-width: 4; indent-tabs-mode: t; -*-

from __future__ import print_function
import os
import sys
import argparse
import clang.cindex

IOS_DEFINES = ["HOST_DARWIN", "TARGET_MACH", "MONO_CROSS_COMPILE", "USE_MONO_CTX", "_XOPEN_SOURCE"]
ANDROID_DEFINES = ["HOST_ANDROID", "MONO_CROSS_COMPILE", "USE_MONO_CTX", "BIONIC_IOCTL_NO_SIGNEDNESS_OVERLOAD"]
LINUX_DEFINES = ["HOST_LINUX", "MONO_CROSS_COMPILE", "USE_MONO_CTX"]
WASI_DEFINES = ["_WASI_EMULATED_PROCESS_CLOCKS", "_WASI_EMULATED_SIGNAL", "_WASI_EMULATED_MMAN"]

class Target:
	def __init__(self, arch, platform, others):
		self.arch_define = arch
		self.platform_define = platform
		self.defines = others

	def get_clang_args(self):
		ret = []
		if self.arch_define:
			ret.append (self.arch_define)
		if self.platform_define:
			if isinstance(self.platform_define, list):
				ret.extend (self.platform_define)
			else:
				ret.append (self.platform_define)
		if self.defines:
			ret.extend (self.defines)
		return ret

class TypeInfo:
	def __init__(self, name, is_jit):
		self.name = name
		self.is_jit = is_jit
		self.size = -1
		self.fields = []

class FieldInfo:
	def __init__(self, name, offset):
		self.name = name
		self.offset = offset

class OffsetsTool:
	def __init__(self):
		pass

	def parse_args(self):
		def require_sysroot (args):
			if not args.sysroot:
				print ("Sysroot dir for device not set.", file=sys.stderr)
				sys.exit (1)

		def require_emscipten_path (args):
			if not args.emscripten_path:
				print ("Emscripten sdk dir not set.", file=sys.stderr)
				sys.exit (1)

		parser = argparse.ArgumentParser ()
		parser.add_argument ('--libclang', dest='libclang', help='path to shared library of libclang.{so,dylib}', required=True)
		parser.add_argument ('--emscripten-sdk', dest='emscripten_path', help='path to emscripten sdk')
		parser.add_argument ('--wasi-sdk', dest='wasi_path', help='path to wasi sdk')
		parser.add_argument ('--outfile', dest='outfile', help='path to output file', required=True)
		parser.add_argument ('--monodir', dest='mono_path', help='path to mono source tree', required=True)
		parser.add_argument ('--nativedir', dest='native_path', help='path to src/native', required=True)
		parser.add_argument ('--targetdir', dest='target_path', help='path to mono tree configured for target', required=True)
		parser.add_argument ('--abi=', dest='abi', help='ABI triple to generate', required=True)
		parser.add_argument ('--sysroot=', dest='sysroot', help='path to sysroot headers of target')
		parser.add_argument ('--prefix=', dest='prefixes', action='append', help='prefix path to include directory of target')
		parser.add_argument ('--netcore', dest='netcore', help='target runs with netcore', action='store_true')
		args = parser.parse_args ()

		if not args.libclang or not os.path.isfile (args.libclang):
			print ("Libclang '" + args.libclang + "' doesn't exist.", file=sys.stderr)
			sys.exit (1)
		if not os.path.isdir (args.mono_path):
			print ("Mono directory '" + args.mono_path + "' doesn't exist.", file=sys.stderr)
			sys.exit (1)
		if not os.path.isfile (args.target_path + "/config.h"):
			print ("File '" + args.target_path + "/config.h' doesn't exist.", file=sys.stderr)
			sys.exit (1)

		self.sys_includes=[]
		self.target = None
		self.target_args = []
		android_api_level = "-D__ANDROID_API=21"

		if "wasm" in args.abi:
			if args.wasi_path != None:
				self.sys_includes = [args.wasi_path + "/share/wasi-sysroot/include", args.wasi_path + "/lib/clang/16/include", args.mono_path + "/wasi/mono-include"]
				self.target = Target ("TARGET_WASI", None, ["TARGET_WASM"] + WASI_DEFINES)
				self.target_args += ["-target", args.abi]
			else:
				require_emscipten_path (args)
				clang_path = os.path.dirname(args.libclang)
				self.sys_includes = [args.emscripten_path + "/system/include", args.emscripten_path + "/system/include/libc", args.emscripten_path + "/system/lib/libc/musl/arch/emscripten", args.emscripten_path + "/system/lib/libc/musl/include", args.emscripten_path + "/system/lib/libc/musl/arch/generic",
									 clang_path + "/../lib/clang/16/include"]
				self.target = Target ("TARGET_WASM", None, [])
				self.target_args += ["-target", args.abi]

		# Linux
		elif "arm-linux-gnueabihf" == args.abi:
			require_sysroot (args)
			self.target = Target ("TARGET_ARM", None, ["ARM_FPU_VFP", "HAVE_ARMV5", "HAVE_ARMV6", "HAVE_ARMV7"] + LINUX_DEFINES)
			self.target_args += ["--target=arm---gnueabihf"]
			self.target_args += ["-I", args.sysroot + "/include"]

			if args.prefixes:
				for prefix in args.prefixes:
					if not os.path.isdir (prefix):
						print ("provided path via --prefix (\"" + prefix + "\") doesn't exist.", file=sys.stderr)
						sys.exit (1)
					self.target_args += ["-I", prefix + "/include"]
					self.target_args += ["-I", prefix + "/include-fixed"]
			else:
				found = False
				for i in range (11, 5, -1):
					prefix = "/usr/lib/gcc-cross/" + args.abi +  "/" + str (i)
					if not os.path.isdir (prefix):
						continue
					found = True
					self.target_args += ["-I", prefix + "/include"]
					self.target_args += ["-I", prefix + "/include-fixed"]
					break

				if not found:
					print ("could not find a valid include path for target, provide one via --prefix=<path>.", file=sys.stderr)
					sys.exit (1)

		elif "aarch64-linux-gnu" == args.abi:
			require_sysroot (args)
			self.target = Target ("TARGET_ARM64", None, LINUX_DEFINES)
			self.target_args += ["--target=aarch64-linux-gnu"]
			self.target_args += ["--sysroot", args.sysroot]
			self.target_args += ["-I", args.sysroot + "/include"]
			if args.prefixes:
				for prefix in args.prefixes:
					if not os.path.isdir (prefix):
						print ("provided path via --prefix (\"" + prefix + "\") doesn't exist.", file=sys.stderr)
						sys.exit (1)
					self.target_args += ["-I", prefix + "/include"]
					self.target_args += ["-I", prefix + "/include-fixed"]

		# iOS/tvOS
		elif "aarch64-apple-darwin10" == args.abi:
			require_sysroot (args)
			self.target = Target ("TARGET_ARM64", ["TARGET_IOS", "TARGET_TVOS"], IOS_DEFINES)
			self.target_args += ["-arch", "arm64"]
			self.target_args += ["-isysroot", args.sysroot]
		elif "x86_64-apple-darwin10" == args.abi:
			require_sysroot (args)
			self.target = Target ("TARGET_AMD64", "", IOS_DEFINES)
			self.target_args += ["-arch", "x86_64"]
			self.target_args += ["-isysroot", args.sysroot]

		# MacCatalyst
		elif "x86_64-apple-maccatalyst" == args.abi:
			require_sysroot (args)
			self.target = Target ("TARGET_AMD64", "TARGET_MACCAT", IOS_DEFINES)
			self.target_args += ["-target", "x86_64-apple-ios13.5-macabi"]
			self.target_args += ["-isysroot", args.sysroot]

		elif "aarch64-apple-maccatalyst" == args.abi:
			require_sysroot (args)
			self.target = Target ("TARGET_ARM64", "TARGET_MACCAT", IOS_DEFINES)
			self.target_args += ["-target", "arm64-apple-ios14.2-macabi"]
			self.target_args += ["-isysroot", args.sysroot]

		# watchOS
		elif "armv7k-apple-darwin" == args.abi:
			require_sysroot (args)
			self.target = Target ("TARGET_ARM", "TARGET_WATCHOS", ["ARM_FPU_VFP", "HAVE_ARMV5"] + IOS_DEFINES)
			self.target_args += ["-arch", "armv7k"]
			self.target_args += ["-isysroot", args.sysroot]
		elif "aarch64-apple-darwin10_ilp32" == args.abi:
			require_sysroot (args)
			self.target = Target ("TARGET_ARM64", "TARGET_WATCHOS", ["MONO_ARCH_ILP32"] + IOS_DEFINES)
			self.target_args += ["-arch", "arm64_32"]
			self.target_args += ["-isysroot", args.sysroot]

		# Android
		elif "i686-none-linux-android" == args.abi:
			require_sysroot (args)
			self.target = Target ("TARGET_X86", "TARGET_ANDROID", ANDROID_DEFINES)
			self.target_args += ["--target=i386---android"]
			self.target_args += ["-I", args.sysroot + "/usr/include/i686-linux-android"]
		elif "x86_64-none-linux-android" == args.abi:
			require_sysroot (args)
			self.target = Target ("TARGET_AMD64", "TARGET_ANDROID", ANDROID_DEFINES)
			self.target_args += ["--target=x86_64---android"]
			self.target_args += ["-I", args.sysroot + "/usr/include/x86_64-linux-android"]
		elif "armv7-none-linux-androideabi" == args.abi:
			require_sysroot (args)
			self.target = Target ("TARGET_ARM", "TARGET_ANDROID", ["ARM_FPU_VFP", "HAVE_ARMV5", "HAVE_ARMV6", "HAVE_ARMV7"] + ANDROID_DEFINES)
			self.target_args += ["--target=arm---androideabi"]
			self.target_args += ["-I", args.sysroot + "/usr/include/arm-linux-androideabi"]
		elif "aarch64-v8a-linux-android" == args.abi:
			require_sysroot (args)
			self.target = Target ("TARGET_ARM64", "TARGET_ANDROID", ANDROID_DEFINES)
			self.target_args += ["--target=aarch64---android"]
			self.target_args += ["-I", args.sysroot + "/usr/include/aarch64-linux-android"]

		if self.target.platform_define == "TARGET_ANDROID":
			self.target_args += [android_api_level]
			self.target_args += ["-isysroot", args.sysroot]

		if not self.target:
			print ("ABI '" + args.abi + "' is not supported.", file=sys.stderr)
			sys.exit (1)

		self.args = args

	#
	# Collect size/alignment/offset information by running clang on files from the runtime
	#
	def run_clang(self):
		args = self.args

		self.runtime_types = {}

		mono_includes = [
			args.mono_path,
			args.mono_path + "/mono",
			args.mono_path + "/mono/eglib",
			args.native_path,
			args.native_path + "/public",
			args.target_path,
			args.target_path + "/mono/eglib"
			]

		self.basic_types = ["gint8", "gint16", "gint32", "gint64", "float", "double", "gpointer"]
		self.runtime_type_names = [
			"MonoObject",
			"MonoClass",
			"MonoVTable",
			"MonoDelegate",
			"MonoInternalThread",
			"MonoMulticastDelegate",
			"MonoTransparentProxy",
			"MonoRealProxy",
			"MonoRemoteClass",
			"MonoArray",
			"MonoArrayBounds",
			"MonoSafeHandle",
			"MonoHandleRef",
			"MonoString",
			"MonoException",
			"MonoTypedRef",
			"MonoThreadsSync",
			"SgenThreadInfo",
			"SgenClientThreadInfo",
			"MonoProfilerCallContext",
		]
		self.jit_type_names = [
			"MonoLMF",
			"MonoLMFExt",
			"MonoMethodILState",
			"MonoMethodRuntimeGenericContext",
			"MonoJitTlsData",
			"MonoGSharedVtMethodRuntimeInfo",
			"MonoContinuation",
			"MonoContext",
			"MonoDelegateTrampInfo",
			"GSharedVtCallInfo",
			"SeqPointInfo",
			"DynCallArgs",
			"MonoLMFTramp",
			"CallContext",
			"MonoFtnDesc"
		]
		for name in self.runtime_type_names:
			self.runtime_types [name] = TypeInfo (name, False)
		for name in self.jit_type_names:
			self.runtime_types [name] = TypeInfo (name, True)

		self.basic_type_size = {}
		self.basic_type_align = {}

		srcfiles = ['mono/metadata/metadata-cross-helpers.c', 'mono/mini/mini-cross-helpers.c']

		clang_args = []
		clang_args += self.target_args
		clang_args += ['-std=gnu11', '-DMONO_GENERATING_OFFSETS']
		for include in self.sys_includes:
			clang_args.append ("-isystem")
			clang_args.append (include)
		for include in mono_includes:
			clang_args.append ("-I")
			clang_args.append (include)
		for define in self.target.get_clang_args ():
			clang_args.append ("-D" + define)

		clang.cindex.Config.set_library_file (args.libclang)

		for srcfile in srcfiles:
			src = args.mono_path + "/" + srcfile
			file_args = clang_args[:]
			if not 'mini' in src:
				file_args.append ('-DHAVE_SGEN_GC')
				file_args.append ('-DHAVE_MOVING_COLLECTOR')
				is_jit = False
			else:
				is_jit = True
			index = clang.cindex.Index.create()
			print ("Running clang: " + ' '.join (file_args) + ' ' + src + '\n')
			tu = index.parse (src, args = file_args)
			for d in tu.diagnostics:
				print (d)
				if d.severity > 2:
					sys.exit (1)
			for c in tu.cursor.walk_preorder():
				if c.kind != clang.cindex.CursorKind.STRUCT_DECL and c.kind != clang.cindex.CursorKind.TYPEDEF_DECL:
					continue
				name = c.spelling
				if c.kind == clang.cindex.CursorKind.TYPEDEF_DECL:
					for c2 in c.get_children ():
						if c2.kind == clang.cindex.CursorKind.STRUCT_DECL or c2.kind == clang.cindex.CursorKind.UNION_DECL:
							c = c2
				type = c.type
				if "struct _" in name:
					name = name [8:]
				if len (name) > 0 and name [0] == '_':
					name = name [1:]
				if name in self.runtime_types:
					rtype = self.runtime_types [name]
					if rtype.is_jit != is_jit:
						continue
					if type.get_size () < 0:
						continue
					rtype.size = type.get_size ()
					for child in c.get_children ():
						if child.kind != clang.cindex.CursorKind.FIELD_DECL:
							continue
						if child.is_bitfield ():
							continue
						rtype.fields.append (FieldInfo (child.spelling, child.get_field_offsetof () // 8))
				if c.spelling == "basic_types_struct":
					for field in c.get_children ():
						btype = field.spelling.replace ("_f", "")
						self.basic_type_size [btype] = field.type.get_size ()
						self.basic_type_align [btype] = field.type.get_align ()

	def gen (self):
		outfile = self.args.outfile
		target = self.target
		f = open (outfile, 'w')
		f.write ("#ifndef USED_CROSS_COMPILER_OFFSETS\n")
		if target.arch_define:
			f.write ("#ifdef " + target.arch_define + "\n")
		if target.platform_define:
			if isinstance(target.platform_define, list):
				f.write ("#if " + " || ".join (["defined (" + platform_define + ")" for platform_define in target.platform_define]) + "\n")
			else:
				f.write ("#ifdef " + target.platform_define + "\n")
		f.write ("#ifndef HAVE_BOEHM_GC\n")
		f.write ("#define HAS_CROSS_COMPILER_OFFSETS\n")
		f.write ("#if defined (USE_CROSS_COMPILE_OFFSETS) || defined (MONO_CROSS_COMPILE)\n")

		f.write ("#if !defined (DISABLE_METADATA_OFFSETS)\n")
		f.write ("#define USED_CROSS_COMPILER_OFFSETS\n")
		for btype in self.basic_types:
			f.write ("DECL_ALIGN2(%s,%s)\n" % (btype, self.basic_type_align [btype]))
		for btype in self.basic_types:
			f.write ("DECL_SIZE2(%s,%s)\n" % (btype, self.basic_type_size [btype]))
		for type_name in self.runtime_type_names:
			type = self.runtime_types [type_name]
			if type.size == -1:
				continue
			f.write ("DECL_SIZE2(%s,%s)\n" % (type.name, type.size))
			done_fields = {}
			for field in type.fields:
				if field.name not in done_fields:
					f.write ("DECL_OFFSET2(%s,%s,%s)\n" % (type.name, field.name, field.offset))
					done_fields [field.name] = field.name
		f.write ("#endif //disable metadata check\n")

		f.write ("#ifndef DISABLE_JIT_OFFSETS\n")
		f.write ("#define USED_CROSS_COMPILER_OFFSETS\n")
		for type_name in self.jit_type_names:
			type = self.runtime_types [type_name]
			if type.size == -1:
				continue
			f.write ("DECL_SIZE2(%s,%s)\n" % (type.name, type.size))
			done_fields = {}
			for field in type.fields:
				if field.name not in done_fields:
					f.write ("DECL_OFFSET2(%s,%s,%s)\n" % (type.name, field.name, field.offset))
					done_fields [field.name] = field.name
		f.write ("#endif //disable jit check\n")

		f.write ("#endif //cross compiler checks\n")
		f.write ("#endif //gc check\n")
		if target.arch_define:
			f.write ("#endif //" + target.arch_define + "\n")
		if target.platform_define:
			if isinstance(target.platform_define, list):
				f.write ("#endif //" + " || ".join (target.platform_define) + "\n")
			else:
				f.write ("#endif //" + target.platform_define + "\n")
		f.write ("#endif //USED_CROSS_COMPILER_OFFSETS check\n")

tool = OffsetsTool ()
tool.parse_args ()
tool.run_clang ()
tool.gen ()
