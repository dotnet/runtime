#!/usr/bin/perl
use strict;
use warnings;
use Cwd;
use POSIX qw(strftime uname locale_h);
use Net::Domain qw(hostname hostfqdn);
use locale;

my $line;
foreach $line (<STDIN>) {
    chomp ($line);
    print "$line\n";
    if ($line =~ /^Overall results:/) {
        # do magic nunit emission here
        # failures look like:
        #    Overall results: tests: 19992, failed: 48, opt combinations: 24 (pass: 99.76%)
        # passes look like:
        #    Overall results: tests: 20928, 100% pass, opt combinations: 24
        my @words = split (/ /, $line);
        my $failed;
        my $successbool;
        my $total = $words[3];
        my $mylocale = setlocale (LC_CTYPE);
        $mylocale = substr($mylocale, 0, index($mylocale, '.'));
        $mylocale =~ s/_/-/;
        if ($line =~ /failed:/) {
            $failed = $words[5];
        } else {
            $failed = "0,";
        }
        chop ($failed);
        chop ($total);
        if ($failed > 0) {
            $successbool = "False";
        } else {
            $successbool = "True";
        }
        open (my $nunitxml, '>', 'TestResults_regression.xml') or die "Could not write to 'TestResults_regression.xml' $!";
        print $nunitxml "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?>\n";
        print $nunitxml "<!--This file represents the results of running a test suite-->\n";
        print $nunitxml "<test-results name=\"regression-tests.dummy\" total=\"$total\" failures=\"$failed\" not-run=\"0\" date=\"" . strftime ("%F", localtime) . "\" time=\"" . strftime ("%T", localtime) . "\">\n";
        print $nunitxml "  <environment nunit-version=\"2.4.8.0\" clr-version=\"4.0.30319.17020\" os-version=\"Unix " . (uname ())[2]  . "\" platform=\"Unix\" cwd=\"" . getcwd . "\" machine-name=\"" . hostname . "\" user=\"" . getpwuid ($<) . "\" user-domain=\"" . hostfqdn  . "\" />\n";
        print $nunitxml "  <culture-info current-culture=\"$mylocale\" current-uiculture=\"$mylocale\" />\n";
        print $nunitxml "  <test-suite name=\"regression-tests.dummy\" success=\"$successbool\" time=\"0\" asserts=\"0\">\n";
        print $nunitxml "    <results>\n";
        print $nunitxml "      <test-suite name=\"MonoTests\" success=\"$successbool\" time=\"0\" asserts=\"0\">\n";
        print $nunitxml "        <results>\n";
        print $nunitxml "          <test-suite name=\"regressions\" success=\"$successbool\" time=\"0\" asserts=\"0\">\n";
        print $nunitxml "            <results>\n";
        print $nunitxml "              <test-case name=\"MonoTests.regressions.100percentsuccess\" executed=\"True\" success=\"$successbool\" time=\"0\" asserts=\"0\"";
        if ( $failed > 0) {
        print $nunitxml ">\n";
        print $nunitxml "                <failure>\n";
        print $nunitxml "                  <message><![CDATA[";
        foreach $line (<STDIN>) {
            chomp ($line);
            print "$line\n";
        }
        print $nunitxml "]]></message>\n";
        print $nunitxml "                  <stack-trace>\n";
        print $nunitxml "                  </stack-trace>\n";
        print $nunitxml "                </failure>\n";
        print $nunitxml "              </test-case>\n";
        } else {
        print $nunitxml " />\n";
        }
        print $nunitxml "            </results>\n";
        print $nunitxml "          </test-suite>\n";
        print $nunitxml "        </results>\n";
        print $nunitxml "      </test-suite>\n";
        print $nunitxml "    </results>\n";
        print $nunitxml "  </test-suite>\n";
        print $nunitxml "</test-results>\n";
        close $nunitxml;
    }
}
