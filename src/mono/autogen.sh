libtoolize --automake
automake -a
autoheader
aclocal
autoconf
./configure $*