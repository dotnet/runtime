#!/usr/bin/env python3
import sys
import argparse
import subprocess

#
# Tool to extract configure.ac defines/checks from configure.ac for use with cmake.
# --autoheader -> generate a cmake style config.h.in
# --emit-cmake -> generate a starting cmake file
#

parser = argparse.ArgumentParser (prefix_chars = '-')
parser.add_argument('--autoheader', metavar='FILENAME', help='emit a config.h.in file')
parser.add_argument('--emit-cmake', metavar='FILENAME', help='emit a list of possible variables/configure options')
parser.add_argument('--filter-defines', metavar='FILENAME', help='list of defines to keep')
parser.add_argument('filename', help='path to configure.ac')
args = parser.parse_args ()

if args.filename == None:
    parser.print_help ()
    sys.exit (1)

if args.autoheader == None and args.emit_cmake == None:
    sys.stderr.write ("At least one of --autoheader/--emit-cmake is required.")
    sys.exit (1)

used_defines = None
if args.filter_defines != None:
    used_defines = {}
    with open (args.filter_defines) as f:
        for line in f:
            line = line.strip ()
            used_defines [line] = 1

#'/usr/local/Cellar/autoconf/2.69/bin/autom4te' --language=autoconf  --verbose --trace AC_CONFIG_HEADERS:'$$config_h ||= '"'"'$1'"'"';' --trace AH_OUTPUT:'A: $1 B $2 C;' --trace AC_DEFINE_TRACE_LITERAL:'$$symbol{'"'"'$1'"'"'} = 1;' configure.ac

# Generate a python script from configure.ac
proc = subprocess.Popen (["autom4te", "--language=autoconf", "--verbose", "--trace", "AH_OUTPUT:defs['$1'] = \"\"\"$2\"\"\"\n", "--trace", "AC_CHECK_HEADER:headers['$1'] = 1\n", "--trace", "AC_CHECK_HEADERS:headers['$1'] = 1\n", "--trace", "AC_CHECK_FUNCS:funcs['$1'] = 1\n", args.filename], stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
outs, errs = proc.communicate ()
if proc.returncode != 0:
    print (errs)
    sys.exit (1)

with open ('tmp.py', 'w') as f:
    f.write ('defs = {}\n')
    f.write ('headers = {}\n')
    f.write ('funcs = {}\n')
    f.write (outs)

# Process it
import tmp

tmp.defs["VERSION"] = """/* Version number of package */
#undef VERSION"""

tmp.defs["HAVE_DLFCN_H"] = """/* Define to 1 if you have the <dlfcn.h> header file. */
#undef HAVE_DLFCN_H"""

if args.autoheader != None:
    with open (args.autoheader, 'w') as f:
        for define in tmp.defs.keys ():
            if define == "MONO_CORLIB_VERSION" or define == "VERSION" or define == "MONO_ARCHITECTURE" or define == "DISABLED_FEATURES" or define.startswith ("SIZEOF_") or define.startswith ("TARGET_SIZEOF_") or define.startswith ("TARGET_BYTE_ORDER"):
                f.write (tmp.defs [define].replace ("#undef " + define, "#define " + define + " @" + define + "@"))
            elif False and define.startswith ("ENABLE_"):
                f.write (tmp.defs [define].replace ('#undef', '#cmakedefine01'))
            else:
                f.write (tmp.defs [define].replace ('#undef ' + define, '#cmakedefine ' + define + ' 1'))
            f.write ('\n\n')

emitted_defines = {}

if args.emit_cmake != None:
    with open (args.emit_cmake, 'w') as f:
        for value in tmp.headers.keys ():
            for header in value.split (" "):
                header = header.strip ()
                if header == "":
                    continue
                define = "HAVE_" + header.replace ("/", "_").replace (".", "_").replace ("-", "_").upper ()
                emitted_defines [define] = 1
                if used_defines != None and not define in used_defines:
                    continue
                f.write ("check_include_file (\"" + header + "\" " + define + ")\n")

        for value in tmp.funcs.keys ():
            for func in value.split (" "):
                func = func.strip ()
                if func == "":
                    continue
                define = "HAVE_" + func.replace ("/", "_").replace (".", "_").replace ("-", "_").upper ()
                emitted_defines [define] = 1
                if used_defines != None and not define in used_defines:
                    continue
                f.write ("check_function_exists (\"" + func + "\" " + define + ")\n")
        for pindex in range(2):
            if pindex == 0:
                f.write ("### Configure defines\n")
            else:
                f.write ("### User defines\n")
            for define in tmp.defs.keys ():
                if define in emitted_defines:
                    continue
                if define.startswith ("HAVE_") and used_defines != None and not define in used_defines:
                    continue
                val = tmp.defs [define]
                is_def = "Define to 1 if" in val
                if (pindex == 0 and is_def) or (pindex == 1 and not is_def):
                    f.write ("option (" + define + " \"" + tmp.defs [define].split (" */")[0].replace ("/*", "").replace ("\"", "").strip () + "\")\n")


