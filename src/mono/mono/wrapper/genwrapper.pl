#!/usr/bin/perl

# Author:
#	Dietmar Maurer (dietmar@ximian.com)
#
# (C) 2001 Ximian, Inc.

use Getopt::Long;
init();

if ($ENV{"OSTYPE"} eq "cygwin") {
@includes = ("sys/types.h", "sys/stat.h", "unistd.h", "fcntl.h", "glib.h",
	     "errno.h");
} else {
@includes = ("sys/types.h", "sys/stat.h", "unistd.h", "fcntl.h", "glib.h",
	     "errno.h");
}

$cflags = `pkg-config --cflags glib-2.0`;
$cflags =~ s/\n//;

$lib = "monowrapper";

if ($csmode){
   $res_struct .= "[CLSCompliant(false)]\n";
}
create_struct ("MonoWrapperStat", "stat",
	       "uint", "mst_dev",
	       "uint", "mst_mode",
	       "uint", "mst_nlink",
	       "uint", "mst_uid",
	       "uint", "mst_gid",
	       "long", "mst_size",
	       "uint", "mst_atime",
	       "uint", "mst_mtime",
	       "uint", "mst_ctime",
	       );

create_func ($lib, "", "seek", "long", 
	     "IntPtr", "fd",
	     "long", "offset",
	     "int", "whence");

create_func ($lib, "", "mkdir", "int",
	     "string", "path",
	     "int",    "mode");

create_func ($lib, "", "rmdir", "int",
	     "string", "path");

create_func ($lib, "", "read", "int",
	     "IntPtr", "fd",
	     "void *", "buf",
	     "int", "count");

create_func ($lib, "", "write", "int",
	     "IntPtr", "fd",
	     "void *", "buf",
	     "int", "count");

create_func ($lib, "", "fstat", "int",
	     "IntPtr", "fd",
	     "stat *", "buf");

create_func ($lib, "", "ftruncate", "int",
	     "IntPtr", "fd",
	     "long", "length");

create_func ($lib, "", "open", "IntPtr",
	     "string", "path",
	     "int", "flags",
	     "int", "mode");

create_func ($lib, "", "close", "int",
	     "IntPtr", "fd");

create_func ($lib, "", "stat", "int",
	     "string", "path",
	     "stat *", "buf");

create_func ($lib, "", "unlink", "int",
	     "string", "path");

create_func ($lib, "", "opendir", "IntPtr",
	     "string", "path");

create_func ($lib, "", "readdir", "string",
	     "IntPtr", "dir");

create_func ($lib, "", "closedir", "int",
	     "IntPtr", "dir");

create_func ($lib, "", "getenv", "IntPtr",
	     "string", "variable");

create_func ($lib, "", "environ", "IntPtr");

create_func ($lib, "", "rename", "int",
	     "string", "source",
	     "string", "target");

create_func ($lib, "", "utime",  "int",
             "string", "path",
	     "int",    "atime",
	     "int",    "mtime");

create_func ($lib, "mono_glob_compile", "mono_glob_compile", "IntPtr",
	     "string", "glob");

create_func ($lib, "mono_glob_match", "mono_glob_match", "int",
	     "IntPtr", "handle",
	     "string", "str");

create_func ($lib, "mono_glob_dispose", "mono_glob_dispose", "void",
	     "IntPtr", "handle");

map_const ("int", "%d", "SEEK_SET",
	   "int", "%d", "SEEK_CUR",
	   "int", "%d", "SEEK_END",

	   "int", "0x%08x", "O_RDONLY",
	   "int", "0x%08x", "O_WRONLY",
	   "int", "0x%08x", "O_RDWR",
	   "int", "0x%08x", "O_CREAT",
	   "int", "0x%08x", "O_EXCL",
	   "int", "0x%08x", "O_NOCTTY",
	   "int", "0x%08x", "O_TRUNC",
	   "int", "0x%08x", "O_SYNC",
	   "int", "0x%08x", "O_APPEND",

	   "int", "0x%08x", "STDIN_FILENO",
	   "int", "0x%08x", "STDOUT_FILENO",
	   "int", "0x%08x", "STDERR_FILENO",

	   "int", "0x%08x", "S_IFMT",
	   "int", "0x%08x", "S_IFSOCK",
	   "int", "0x%08x", "S_IFLNK",
	   "int", "0x%08x", "S_IFREG",
	   "int", "0x%08x", "S_IFBLK",
	   "int", "0x%08x", "S_IFDIR",
	   "int", "0x%08x", "S_IFCHR",
	   "int", "0x%08x", "S_IFIFO",
	   "int", "0x%08x", "S_ISUID",
	   "int", "0x%08x", "S_ISGID",
	   "int", "0x%08x", "S_ISVTX",
	   "int", "0x%08x", "S_IRWXU",
	   "int", "0x%08x", "S_IRUSR",
	   "int", "0x%08x", "S_IWUSR",
	   "int", "0x%08x", "S_IXUSR",
	   "int", "0x%08x", "S_IRWXG",
	   "int", "0x%08x", "S_IRGRP",
	   "int", "0x%08x", "S_IWGRP",
	   "int", "0x%08x", "S_IXGRP",
	   "int", "0x%08x", "S_IRWXO",
	   "int", "0x%08x", "S_IROTH",
	   "int", "0x%08x", "S_IWOTH",
	   "int", "0x%08x", "S_IXOTH",

	   "int", "%d", "EPERM",
	   "int", "%d", "ENOENT",
	   "int", "%d", "ESRCH",
	   "int", "%d", "EINTR",
	   "int", "%d", "EIO",
	   "int", "%d", "ENXIO",
	   "int", "%d", "E2BIG",
	   "int", "%d", "ENOEXEC",
	   "int", "%d", "EBADF",
	   "int", "%d", "ECHILD",
	   "int", "%d", "EAGAIN",
	   "int", "%d", "ENOMEM",
	   "int", "%d", "EACCES",
	   "int", "%d", "EFAULT",
	   "int", "%d", "ENOTBLK",
	   "int", "%d", "EBUSY",
	   "int", "%d", "EEXIST",
	   "int", "%d", "EXDEV",
	   "int", "%d", "ENODEV",
	   "int", "%d", "EISDIR",
	   "int", "%d", "EINVAL",
	   "int", "%d", "ENFILE",
	   "int", "%d", "EMFILE",
	   "int", "%d", "ENOTTY",
	   "int", "%d", "ETXTBSY",
	   "int", "%d", "EFBIG",
	   "int", "%d", "ENOSPC",
	   "int", "%d", "ESPIPE",
	   "int", "%d", "EROFS",
	   "int", "%d", "EMLINK",
	   "int", "%d", "EPIPE",
	   "int", "%d", "EDOM",
	   "int", "%d", "ERANGE",
	   "int", "%d", "EDEADLK",
	   "int", "%d", "ENAMETOOLONG",
	   "int", "%d", "ENOLCK",
	   "int", "%d", "ENOSYS",
	   "int", "%d", "ENOTEMPTY",
	   "int", "%d", "ELOOP",
	   "int", "%d", "EWOULDBLOCK",
	   "int", "%d", "ENOMSG",
	   "int", "%d", "EIDRM",
	   "int", "%d", "ECHRNG",
	   "int", "%d", "EL2NSYNC",
	   "int", "%d", "EL3HLT",
	   "int", "%d", "EL3RST",
	   "int", "%d", "ELNRNG",
	   "int", "%d", "EUNATCH",
	   "int", "%d", "ENOCSI",
	   "int", "%d", "EL2HLT",
	   "int", "%d", "EBADE",
	   "int", "%d", "EBADR",
	   "int", "%d", "EXFULL",
	   "int", "%d", "ENOANO",
	   "int", "%d", "EBADRQC",
	   "int", "%d", "EBADSLT",
	   "int", "%d", "EDEADLOCK",
	   "int", "%d", "EBFONT",
	   "int", "%d", "ENOSTR",
	   "int", "%d", "ENODATA",
	   "int", "%d", "ETIME",
	   "int", "%d", "ENOSR",
	   "int", "%d", "ENONET",
	   "int", "%d", "ENOPKG",
	   "int", "%d", "EREMOTE",
	   "int", "%d", "ENOLINK",
	   "int", "%d", "EADV",
	   "int", "%d", "ESRMNT",
	   "int", "%d", "ECOMM",
	   "int", "%d", "EPROTO",
	   "int", "%d", "EMULTIHOP",
	   "int", "%d", "EDOTDOT",
	   "int", "%d", "EBADMSG",
	   "int", "%d", "ENOTUNIQ",
	   "int", "%d", "EBADFD",
	   "int", "%d", "EREMCHG",
	   "int", "%d", "ELIBACC",
	   "int", "%d", "ELIBBAD",
	   "int", "%d", "ELIBSCN",
	   "int", "%d", "ELIBMAX",
	   "int", "%d", "ELIBEXEC",
	   "int", "%d", "EUSERS",
	   "int", "%d", "ENOTSOCK",
	   "int", "%d", "EDESTADDRREQ",
	   "int", "%d", "EMSGSIZE",
	   "int", "%d", "EPROTOTYPE",
	   "int", "%d", "ENOPROTOOPT",
	   "int", "%d", "EPROTONOSUPPORT",
	   "int", "%d", "ESOCKTNOSUPPORT",
	   "int", "%d", "EOPNOTSUPP",
	   "int", "%d", "EPFNOSUPPORT",
	   "int", "%d", "EAFNOSUPPORT",
	   "int", "%d", "EADDRINUSE",
	   "int", "%d", "EADDRNOTAVAIL",
	   "int", "%d", "ENETDOWN",
	   "int", "%d", "ENETUNREACH",
	   "int", "%d", "ENETRESET",
	   "int", "%d", "ECONNABORTED",
	   "int", "%d", "ECONNRESET",
	   "int", "%d", "ENOBUFS",
	   "int", "%d", "EISCONN",
	   "int", "%d", "ENOTCONN",
	   "int", "%d", "ESHUTDOWN",
	   "int", "%d", "ETOOMANYREFS",
	   "int", "%d", "ETIMEDOUT",
	   "int", "%d", "ECONNREFUSED",
	   "int", "%d", "EHOSTDOWN",
	   "int", "%d", "EHOSTUNREACH",
	   "int", "%d", "EALREADY",
	   "int", "%d", "EINPROGRESS",
	   "int", "%d", "ESTALE",
	   "int", "%d", "EDQUOT",
	   "int", "%d", "ENOMEDIUM",
	   "int", "%d", "ENOTDIR",
	   );

sub init {

    $csmode = 0;
    $defmode = 0;

    GetOptions ("c|csharp" => \$csmode,
		"d|defmode" => \$defmode) or die "cant parse options";

    $CC = $env{"CC"};

    if (!$CC) {
	$CC = "gcc";
    }


    %tmap = ("void" => "void",
	     "IntPtr" => "gpointer",
	     "sbyte" => "gint8",
	     "byte" => "guint8",
	     "short" => "gint16",
	     "ushort" => "guint16",
	     "int" => "gint32",
	     "uint" => "guint32",
	     "long" => "gint64",
	     "ulong" => "guint64",
	     "string" => "const char *",
	     );
}

sub t {
    my ($name) = @_;
    my ($rname) = $name;

    if ($name =~ m/(.*)\*\s*$/) {
	$rname = $1;
	$rname =~ s/\s+$//; # remove trailing spaces
	$rval = $tmap{$rname} || die "unable to map type \"$name\"";
	return "$rval*";
    }

    $rval = $tmap{$name} || die "unable to map type \"$name\"";

}

sub create_func {
    my (@func) = @_;
    my ($i) = 0;
    my ($res) = "";
    my ($cls) = 1;
    my ($j) = 4;
    while ($j <= $#func){
	if ($func[$j] =~ /\*/){
		$cls = 0;
	}
	$j+=2;
    }

    if ($func[1] eq "") {
	$func[1] = "mono_wrapper_$func[2]";
    }

    if ($defmode) {
	$dlldef .= "\t$func[1]\n";
    }

    if ($csmode) {

	$res = "\t[DllImport(\"$func[0]\", EntryPoint=\"$func[1]\", CharSet=CharSet.Ansi)]\n";
	if ($cls == 0){
	   $res .= "\t[CLSCompliant(false)]\n";
        }
	$res .= "\tpublic unsafe static extern $func[3] $func[2] (";
	$i +=4;
	while ($i <= $#func) {
	    if ($i>4) {
		$res .= ", ";
	    }
	    $res .= "$func[$i] $func[$i+1]";
	    
	    $i+=2;
	}
	$res .= ");\n\n";

	$res_func .= $res;

    } else  {
	
	$res = t($func[3]) . "\n$func[1] (";
	
	$i +=4;
	while ($i <= $#func) {
	    if ($i>4) {
		$res .= ", ";
	    }
	    $res .= t($func[$i]) . " $func[$i+1]";
	    
	    $i+=2;
	}
	$res .= ");\n\n";

	$res_func .= $res;
    }
}

sub create_struct {
    my (@str) = @_;
    my ($i) = 0;
    my ($res) = "";

    if ($csmode) {
	$res = "public struct $str[1] {\n";
	$i +=2;
	while ($i <= $#str) {
	    $res .= "\tpublic $str[$i] $str[$i+1];\n";
	    $i+=2;
	}
	$res .= "};\n\n";
    } else {
	$res = "typedef struct {\n";
	$i += 2;
	while ($i <= $#str) {
	    $res .= "\t" . t($str[$i]) . " $str[$i+1];\n";
	    $i+=2;
	}
	$res .= "} $str[0];\n\n";
    }

    $tmap{"$str[1]"} = "$str[0]";

    $res_struct .= $res;
}

sub map_const {
    my (@co) = @_;
    my ($res) = "";
    my ($l);
    my ($space);

    if (!$csmode) {
	return;
    }

    my ($tfn) = "/tmp/etypes$$.c";

    open (TFN, ">$tfn") || die (0);

    for ($i = 0; $i <= $#includes; $i++) {
	print TFN "#include \"$includes[$i]\"\n";
    }
    
    print TFN "\nint main () {\n"; 
    for ($i = 0; $i <= $#co; $i+=3) {

	$l = 20 - length($co[$i+2]);

	$space = "";
	for (my ($j) = 0; $j < $l; $j++) {
	    $space = $space . " ";
	}

	print TFN "printf (\"\\tpublic const %s %s $space= $co[$i+1];\\n\",".
	    " \"$co[$i]\", \"$co[$i+2]\", $co[$i+2]);\n";

    }
    print TFN "exit (-1);\n";
    print TFN "}\n";

    close (TFN);

    system ("$CC $cflags $tfn -o conftest.exe") == 0
	or die "calling c compiler failed";

    system ("rm $tfn");

    $res = `./conftest.exe`;

    if (!$res) {
	die "calling a.out failde";
    }

    $res_const = $res_const . $res;

    system ("rm ./conftest.exe");	
}

sub etypes_end {

    @ae = split (/\./, $__class);

    print $res_struct;

    print "public class $ae[$#ae] {\n\n";

    if ($res_const) {
	print "$res_const\n\n";
    }

    print "$res_func";

    printf "} // class $ae[$#ae]\n\n";

    for ($i = $#ae - 1; $i >= 0; $i--) {
	print "} // namescape $ae[$i]\n";
    }

}

print "/*\n * Generated automatically: do not edit this file.\n */\n\n";


if ($csmode) {

    print "using System;\n";
    print "using System.Runtime.InteropServices;\n\n";

    print "namespace System.Private {\n\n";

    print $res_struct;

    print "public class Wrapper {\n\n";

    if ($res_const) {
	print "$res_const\n\n";
    }

    print "$res_func";

    print "}\n";

    print "}\n";
    
} elsif ($defmode) {
	
    print "LIBRARY libmonowrapper\n";
    print "EXPORTS\n";
    print "\tDllMain\n";
    print $dlldef;

} else {

    print "#ifndef _MONO_WRAPPER_H_\n#define _MONO_WRAPPER_H_ 1\n\n";

    for ($i = 0; $i <= $#includes; $i++) {
	print "#include <$includes[$i]>\n";
    }

    print "\n";

    print $res_struct;

    print $res_func;

    print "#endif\n";
}
