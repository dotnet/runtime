#!/usr/bin/perl
$q = 1;

# Modified by Edwin Lima (edwinlima@hotmail.com; edwin.lima@nec-computers.com)
# Date: 08/21/01, The Netherlands
# $f: Variable used as a flag to create the list of questions on top of the question-answers set. This
# is the only way that I found to scan the questions which have a <CR><LF>, in such a way that I will not
# scan the answer together at same time.
# @aname: Buffer used to store the question-answers set to print them out just after the print of the
# questions.
# @href: Buffer used to store the anchors (only questions) to the questions-answers set on the bottom 
# of the page.
# I opened explicitly the file for input (input.txt) but U can change this as it was originally.
#
#

#comment this line if you are not open the file explicitly
#open(IN, "input.txt") || die "cannot open file input.txt" ; 

print("<A name=TOP>") ;

#Uncomment line bellow to make it work as it was originally.
while (<>){ 

#comment line bellow

#while (<IN>){
	chop;
	if (/^\* (.*)$/){
		push(@aname,"<h1>$1</h1>\n");
#		print $body;
	} elsif (/^\*\* (.*)$/) {
		push(@aname, "<h2>$1</h2>\n");
		push(@href, "<h2>$1</h2>\n");
	} elsif (/^\*\*\* (.*)$/) {
		push(@aname, "<h3>$1</h3>\n");
		
	} elsif (/^$/) {
		push(@aname, "<p>\n");
#		push(@href, "<p>\n");		NOT NEEDED
	} elsif (/^\t\t\* (.*)$/) {
		push(@aname, "<li>$1\n");
	} elsif (/^Q: (.*)$/){
		push(@aname, "<p><a name=\"q$q\"></a><b>Question $q:</b> $1\n");
		push(@href,"<p><a href=\"#q$q\"><b>Question $q:</b></a> $1\n");
		$f=1; 
		$q++;
	} elsif (/^A: (.*)$/){
		push(@aname,"<P>\n<A HREF=#TOP>Top</A>\n<P>");
		push(@aname,"$1\n");
		$f=0;
	} elsif (/^TODO=(.*),$/){
	        push(@aname, "<a name=\"$1\">\n");
	        #push(@href, "<a name=\"$1\">\n");
        } else {
		push(@aname,"$_\n");
		if ($f==1) {
		push(@href,"$_\n");
		}
	}
}

foreach $line (@href) #"\n\n";
{
	print $line;
	}

foreach $line (@aname) #"\n\n";
{
	print $line;
	}


#comment this line if you are not open the file explicitly
#   close(IN) || die "cannot close file" ; 

