#!/bin/sh
top_srcdir=$1
if test -d $top_srcdir/.git; then
	(cd $top_srcdir;
	 LANG=C; export LANG;
	 if test -z "$ghprbPullId"; then
		 branch=`git branch | grep '^\*' | sed 's/(detached from .*/explicit/' | cut -d ' ' -f 2`;
	 else
		 branch="pull-request-$ghprbPullId";
	 fi;
	 version=`git log --no-color --first-parent -n1 --pretty=format:%h`;
	 echo "#define FULL_VERSION \"$branch/$version\"";
	);
else
	echo "#define FULL_VERSION \"tarball\"";
fi > version.h

