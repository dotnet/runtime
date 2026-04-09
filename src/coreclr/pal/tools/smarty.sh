if ! test "$_TGTCPU"
then
	echo !!!ERROR: _TGTCPU not set.  Please run preptests.sh
fi
if ! test "$CORE_RUN"
then
	echo !!!ERROR: CORE_RUN not set.  Please run preptests.sh
fi
if ! test "$CORE_ROOT"
then
	echo !!!ERROR: CORE_ROOT not set.  Please run preptests.sh
fi
if ! test "$BVT_ROOT"
then
	export BVT_ROOT=$PWD
fi

if [ -n "$PERL5LIB" ]; then
    if [ -z "`expr $PERL5LIB : ".*\($BVT_ROOT/Common/Smarty\)"`" ];  then
        export PERL5LIB="$PERL5LIB:$BVT_ROOT/Common/Smarty"
    fi
else
    export PERL5LIB=$BVT_ROOT/Common/Smarty
fi

perl Common/Smarty/Smarty.pl $*
