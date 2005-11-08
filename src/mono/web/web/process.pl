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
  chomp;
  my @command = split /,/;
  if ($command[0] != -1) {
      $menu .= "\t\t";
	  $menu .= "<tr><td valign=\"top\" class=\"navi" . $command[0];
	  $menu .= "\"><a class=\"navi" . $command[0];
	  $menu .= "\"";
	  $menu .= " HREF=\"$command[2]\">$command[1]</a></td></tr>\n\n";
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
  chomp;
  my @command = split /,/;

  if ($command[2] =~ /^http:/){
  } else {
	  $n = $ARGV[2] . "/" . $command[2];
	  open OUTPUT, ">" . $n || die "Can not create $n";
	
	  my $content = "";
	  open INPUT, "src/$command[3]" || die "Can not open $command[3]";
	  while (<INPUT>) {
	    $content .= $_;
	  }
	  close INPUT;
	
	  my $line;
	  my $temp;
	  my $tit;
	  my $title;
	  my $css;
	  my $script;
	
	  $tit = $command[1];
	  $css = $command[4];
	  $script = $command[5];

	  foreach $line (@template) {
	    $temp = $line;
	    $title = "$tit / Mono";
	    $temp =~ s/#TITLE#/$title/;
	    $temp =~ s/#CONTENT#/$content/;
	    $temp =~ s/#MENU#/$menu/;
	    if ($css) {
	      $temp =~ s/#CSS#/<link rel="stylesheet" type="text\/css" href="$css" \/>/;
	    } else {
	      $temp =~ s/#CSS#//;
	    }
		
	    if ($script) {
	      $temp =~ s/#SCRIPT#/<script src="$script"><\/script>/;
	    } else {
	      $temp =~ s/#SCRIPT#//;
	    }
	    print OUTPUT $temp;
	  }
 }	
  close OUTPUT;
}
