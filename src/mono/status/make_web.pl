#!/usr/bin/perl -W

open COMMANDS, ">commands";

print COMMANDS "0,Class Libraries,index.html,index.src\n";

print COMMANDS "0,Namespace,namespace.html,namespace.src\n";
$files_list = `ls bn`;
@files = split(/\s+/, $files_list);
foreach $file (@files) {
	print COMMANDS "1,$file,$file.html,bn/$file\n";
}

print COMMANDS "0,Maintainer,maintainer.html,maintainer.src\n";
$files_list = `ls bm`;
@files = split(/\s+/, $files_list);
foreach $file (@files) {
	print COMMANDS "1,$file,$file.html,bm/$file\n";
}

close COMMANDS
