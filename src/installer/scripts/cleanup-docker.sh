#!/usr/bin/perl

#
# ./cleanup-docker.sh 
#

# cleanup containers
my $psList = `docker ps -a`;
my @psItems = split /\n/, $psList;
foreach(@psItems) {
  # match 'docker ps' output to capture the container name
  if($_ =~ /.*\s+([^\s]+)$/ig) {
    my $containerName = $1;
    if($containerName !~ /NAME/ig) {
      printf "delete container $containerName\n";
      my $deleteOutput = `docker rm -f $1`;
      print "$deleteOutput\n";
    }
  }
}

#cleanup images
my $imageList = `docker images`;
@imageItems = split /\n/, $imageList;
foreach(@imageItems) {
  # match 'docker images' output to capture the image id
  if($_ =~ /([^\s]+)\s+([^\s]+)\s+([^\s]+)\s+.*/ig) {
    my $imageId = $3;
    if($imageId !~ /IMAGE/ig) {
      printf "delete image ID $imageId\n";
      my $deleteImageOutput = `docker rmi -f $imageId`;
      printf "$deleteImageOutput\n";
    }
  }
}
