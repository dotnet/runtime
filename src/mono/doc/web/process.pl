#!/usr/bin/perl
#
# Author:
#   Sean MacIsaac
#

use strict;

my $full_expand = 1;
my @template;
my $n;

if ($#ARGV != 2) {
  print "process.pl command_file template_file directory_prefix\n";
  exit ();
}

my $menu = "";

open COMMANDS, $ARGV[0] || die "Can not open $ARGV[0]";
while (<COMMANDS>) {
  chop;
  my @command = split /,/;
  if ($command[0] != -1) {
      $menu .= "\t\t";
      if ($command[0] == 0){
	  $menu .= "<tr><td class=\"navi\"><a class=\"navi\"";
      } else {
	  $menu .= "&nbsp; &nbsp; &nbsp; <tr><td class=\"subnavi\"><a class=\"navi\"";
      }
      $menu .= "HREF=\"$command[2]\">$command[1]</A></td></tr>\n\n";
  } 
}
close COMMANDS;

open TEMPLATE, $ARGV[1] || die "Can not open $ARGV[1]";
while (<TEMPLATE>) {
  push @template, $_;
}
close TEMPLATE;

open COMMANDS, $ARGV[0] || die "Can not open $ARGV[0]";
while (<COMMANDS>) {
  chop;
  my @command = split /,/;

  $n = $ARGV[2] . "/" . $command[2];
  open OUTPUT, ">" . $n || die "Can not create $n";

  my $content = "";
  open INPUT, $command[3] || die "Can not open $command[3]";
  while (<INPUT>) {
    $content .= $_;
  }
  close INPUT;

  my $line;
  my $temp;

  foreach $line (@template) {
    $temp = $line;
    $temp =~ s/#CONTENT#/$content/;
    $temp =~ s/#MENU#/$menu/;
    print OUTPUT $temp;
  }

  close OUTPUT;
}
