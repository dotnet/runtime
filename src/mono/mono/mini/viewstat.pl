#!/usr/bin/perl

use GD::Graph::bars;
use GD::Graph::bars3d;
use Getopt::Std;

$Usage = "usage: $0 [-e] [-o file] statfile";

# -e       generate a 3D graph
# -o file  write the graph to file, instead of starting the viewer

getopts('eo:', \%Opts)
    or die "$Usage";
die "$Usage\n"
    unless (@ARGV == 1);

$statfile = shift;

sub save_chart
{
        my $chart = shift or die "Need a chart!";
        my $name = shift or die "Need a name!";
        local(*OUT);

        open(OUT, ">$name") or 
                die "Cannot open $name.$ext for write: $!";
        binmode OUT;
        print OUT $chart->gd->png();
        close OUT;
}


print STDERR "Processing file $statfile\n";

if ($Opts{'e'}) {
    $graph = new GD::Graph::bars3d(800, 600);
} else {
    $graph = new GD::Graph::bars(800, 600);
}
$graph->set( 
	     y_label         => 'Time',
	     y_long_ticks      => 1,

	     x_long_ticks      => 1,

	     y_tick_number   => 8,

	     x_labels_vertical   => 1,
	     bar_spacing         => 5,
	     
	     show_values         => 1,
	     values_vertical     => 1,
	     	     
	     l_margin            => 10,
	     b_margin            => 10,
	     r_margin            => 10,
	     t_margin            => 10,

	     shadow_depth => 1,

	     transparent     => 0,
);

if ($Opts{'e'}) {
    $graph->set (overwrite => 1);
    $graph->set (show_values => 0);
}

require $statfile;

if ($stattitle ne "") {
    $graph->set (title=> $stattitle);
}

$outfile = $Opts{'o'};

if ($outfile eq "") {
    $tmp = $outfile = "/tmp/viewstat" . $$ . ".png";
}

$graph->plot(\@data);
save_chart($graph, $outfile);

if ($tmp) {
    `eog $outfile`;
    `rm $tmp`;
}
