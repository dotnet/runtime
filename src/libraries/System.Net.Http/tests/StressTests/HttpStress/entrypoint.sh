mkdir -p $DUMPS_SHARE_MOUNT_ROOT/$STRESS_ROLE

ulimit -c unlimited

# Enable dump collection
export DOTNET_DbgEnableMiniDump=1
export DOTNET_DbgMiniDumpType=MiniDumpWithFullMemory
export DOTNET_DbgMiniDumpName="$DUMPS_SHARE_MOUNT_ROOT/$STRESS_ROLE/coredump.%p.%t"

/live-runtime-artifacts/testhost/net$VERSION-linux-$CONFIGURATION-x64/dotnet exec --roll-forward Major ./bin/$CONFIGURATION/net$VERSION/HttpStress.dll $STRESS_ARGS

exit_code=$?

if [ $exit_code -ne 0 ]; then
    echo "HttpStress failed, copying artifacts for investigation"

    if [ ! -d "$DUMPS_SHARE_MOUNT_ROOT/net$VERSION-linux-$CONFIGURATION-x64" ] && [ -n "$DUMPS_SHARE_MOUNT_ROOT" ]; then
        # copy runtime artifacts and msquic
        cp -r /live-runtime-artifacts/testhost/net$VERSION-linux-$CONFIGURATION-x64/ $DUMPS_SHARE_MOUNT_ROOT
        mkdir -p $DUMPS_SHARE_MOUNT_ROOT/msquic
        cp /msquic/msquic/build/bin/Debug/libmsquic.so.2 $DUMPS_SHARE_MOUNT_ROOT/msquic
    fi
fi

exit $exit_code
