#!/usr/bin/perl -w

# mono stress test tool
# This stress test runner is designed to detect possible
# leaks, runtime slowdowns and crashes when a task is performed
# repeatedly.
# A stress program should be written to repeat for a number of times
# a specific task: it is run a first time to collect info about memory
# and cpu usage: this run should last a couple of seconds or so.
# Then, the same program is run with a number of iterations that is at least
# 2 orders of magnitude bigger than the first run (3 orders should be used,
# eventually, to detect smaller leaks).
# Of course the right time for the test and the ratio depends on the test
# itself, so it's configurable per-test.
# The test driver will then check that the second run has used roughly the
# same amount of memory as the first and a proportionally bigger cpu time.
# Note: with a conservative GC there may be more false positives than
# with a precise one, because heap size may grow depending on timing etc.
# so failing results need to be checked carefully. In some cases a solution
# is to increase the number of runs in the dry run.

use POSIX ":sys_wait_h";
use Time::HiRes qw(usleep ualarm gettimeofday tv_interval);

# in milliseconds between checks of resource usage
my $interval = 50;
# multiplier to allow some wiggle room
my $wiggle_ratio = 1.05;
# if the test computer is too fast or if we want to stress test more,
# we multiply the test ratio by this number. Use the --times=x option.
my $extra_strong = 1;

# descriptions of the tests to run
# for each test:
#	program is the program to run
#	args an array ref of argumenst to pass to program
#	arg-knob is the index of the argument in args that changes the number of iterations
#	ratio is the multiplier applied to the arg-knob argument
my %tests = (
	'domain-stress' => {
		'program' => 'domain-stress.exe',
		# threads, domains, allocs, loops
		'args' => [2, 10, 1000, 1],
		'arg-knob' => 3, # loops
		'ratio' => 30,
	},
	'gchandle-stress' => {
		'program' => 'gchandle-stress.exe',
		# allocs, loops
		'args' => [80000, 2],
		'arg-knob' => 1, # loops
		'ratio' => 20,
	},
	'monitor-stress' => {
		'program' => 'monitor-stress.exe',
		# loops
		'args' => [10],
		'arg-knob' => 0, # loops
		'ratio' => 20,
	},
	'gc-stress' => {
		'program' => 'gc-stress.exe',
		# loops
		'args' => [25],
		'arg-knob' => 0, # loops
		'ratio' => 20,
	},
	'gc-graystack-stress' => {
		'program' => 'gc-graystack-stress.exe',
		# width, depth, collections
		'args' => [125, 10000, 100],
		'arg-knob' => 2, # loops
		'ratio' => 10,
	},
	'gc-copy-stress' => {
		'program' => 'gc-copy-stress.exe',
		# loops, count, persist_factor
		'args' => [250, 500000, 10],
		'arg-knob' => 1, # count
		'ratio' => 4,
	},
	'thread-stress' => {
		'program' => 'thread-stress.exe',
		# loops
		'args' => [20],
		'arg-knob' => 0, # loops
		'ratio' => 20,
	},
	'abort-stress-1' => {
		'program' => 'abort-stress-1.exe',
		# loops,
		'args' => [20],
		'arg-knob' => 0, # loops
		'ratio' => 20,
	},
	# FIXME: This tests exits, so it has no loops, instead it should be run more times
	'exit-stress' => {
		'program' => 'exit-stress.exe',
		# loops,
		'args' => [10],
		'arg-knob' => 0, # loops
		'ratio' => 20,
	}
	# FIXME: This test deadlocks, bug 72740.
	# We need hang detection
	#'abort-stress-2' => {
	#	'program' => 'abort-stress-2.exe',
	#	# loops,
	#	'args' => [20],
	#	'arg-knob' => 0, # loops
	#	'ratio' => 20,
	#}
);

# poor man option handling
while (@ARGV) {
	my $arg = shift @ARGV;
	if ($arg =~ /^--times=(\d+)$/) {
		$extra_strong = $1;
		next;
	}
	if ($arg =~ /^--interval=(\d+)$/) {
		$interval = $1;
		next;
	}
	unshift @ARGV, $arg;
	last;
}
my $test_rx = shift (@ARGV) || '.';
# the mono runtime to use and the arguments to pass to it
my @mono_args = @ARGV;
my @results = ();
my %vmmap = qw(VmSize 0 VmLck 1 VmRSS 2 VmData 3 VmStk 4 VmExe 5 VmLib 6 VmHWM 7 VmPTE 8 VmPeak 9);
my @vmnames = sort {$vmmap{$a} <=> $vmmap{$b}} keys %vmmap;
# VmRSS depends on the operating system's decisions
my %vmignore = qw(VmRSS 1);
my $errorcount = 0;
my $numtests = 0;

@mono_args = 'mono' unless @mono_args;

apply_options ();

foreach my $test (sort keys %tests) {
	next unless ($tests{$test}->{'program'} =~ /$test_rx/);
	$numtests++;
	run_test ($test, 'dry');
	run_test ($test, 'stress');
}

# print all the reports at the end
foreach my $test (sort keys %tests) {
	next unless ($tests{$test}->{'program'} =~ /$test_rx/);
	print_test_report ($test);
}

print "No tests matched '$test_rx'.\n" unless $numtests;

if ($errorcount) {
	print "Total issues: $errorcount\n";
	exit (1);
} else {
	exit (0);
}

sub run_test {
	my ($name, $mode) = @_;
	my $test = $tests {$name};
	my @targs = (@mono_args, $test->{program});
	my @results = ();
	my @rargs = @{$test->{"args"}};

	if ($mode ne "dry") {
		# FIXME: set also a timeout
		$rargs [$test->{"arg-knob"}] *= $test->{"ratio"};
	}
	push @targs, @rargs;
	print "Running test '$name' in $mode mode\n";
	my $start_time = [gettimeofday];
	my $pid = fork ();
	if ($pid == 0) {
		exec @targs;
		die "Cannot exec: $! (@targs)\n";
	} else {
		my $kid;
		do {
			$kid = waitpid (-1, WNOHANG);
			my $sample = collect_memusage ($pid);
			push @results, $sample if (defined ($sample) && @{$sample});
			# sleep for a few ms
			usleep ($interval * 1000) unless $kid > 0;
		} until $kid > 0;
	}
	my $end_time = [gettimeofday];
	$test->{"$mode-cputime"} = tv_interval ($start_time, $end_time);
	$test->{"$mode-memusage"} = [summarize_result (@results)];
}

sub print_test_report {
	my ($name) = shift;
	my $test = $tests {$name};
	my ($cpu_dry, $cpu_test) = ($test->{'dry-cputime'}, $test->{'stress-cputime'});
	my @dry_mem = @{$test->{'dry-memusage'}};
	my @test_mem = @{$test->{'stress-memusage'}};
	my $ratio = $test->{'ratio'};
	print "Report for test: $name\n";
	print "Cpu usage: dry: $cpu_dry, stress: $cpu_test\n";
	print "Memory usage (KB):\n";
	print "\t       ",join ("\t", @vmnames), "\n";
	print "\t   dry: ", join ("\t", @dry_mem), "\n";
	print "\tstress: ", join ("\t", @test_mem), "\n";
	if ($cpu_test > ($cpu_dry * $ratio) * $wiggle_ratio) {
		print "Cpu usage not proportional to ratio $ratio.\n";
		$errorcount++;
	}
	my $i;
	for ($i = 0; $i < @dry_mem; ++$i) {
		next if exists $vmignore {$vmnames [$i]};
		if ($test_mem [$i] > $dry_mem [$i] * $wiggle_ratio) {
			print "Memory usage $vmnames[$i] not constant.\n";
			$errorcount++;
		}
	}
}

sub collect_memusage {
	my ($pid) = @_;
	open (PROC, "</proc/$pid/status") || return undef; # might be dead already
	my @sample = ();
	while (<PROC>) {
		next unless /^(Vm.*?):\s+(\d+)\s+kB/;
		$sample [$vmmap {$1}] = $2;
	}
	close (PROC);
	return \@sample;
}

sub summarize_result {
	my (@data) = @_;
	my (@result) = (0) x 7;
	my $i;
	foreach my $sample (@data) {
		for ($i = 0; $i < 7; ++$i) {
			if ($sample->[$i] > $result [$i]) {
				$result [$i] = $sample->[$i];
			}
		}
	}
	return @result;
}

sub apply_options {
	foreach my $test (values %tests) {
		$test->{args}->[$test->{'arg-knob'}] *= $extra_strong;
	}
}

