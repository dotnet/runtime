libtoolize --automake
automake -a
autoheader
aclocal $ACLOCAL_FLAGS
autoconf
./configure $*
