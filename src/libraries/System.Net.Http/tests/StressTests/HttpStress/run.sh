
cp /live-runtime-artifacts/System.IO.Pipelines/$CONFIGURATION/net$VERSION/System.IO.Pipelines.dll /live-runtime-artifacts/testhost/net$VERSION-linux-$CONFIGURATION-x64/shared/Microsoft.AspNetCore.App/*/

cp ./Microsoft.AspNetCore.Server.Kestrel.Core.* /live-runtime-artifacts/testhost/net$VERSION-linux-$CONFIGURATION-x64/shared/Microsoft.AspNetCore.App/*/

/live-runtime-artifacts/testhost/net$VERSION-linux-$CONFIGURATION-x64/dotnet exec --roll-forward Major ./bin/$CONFIGURATION/net$VERSION/HttpStress.dll $HTTPSTRESS_ARGS

if [ $? -ne 0 ]; then
    echo "HttpStress failed"
    # copy runtime artifacts and msquic
    cp -r /live-runtime-artifacts/testhost/net$VERSION-linux-$CONFIGURATION-x64/ $DUMPS_SHARE_MOUNT_ROOT
    mkdir -p $DUMPS_SHARE_MOUNT_ROOT/msquic
    cp /msquic/msquic/build/bin/Debug/libmsquic.so.2 $DUMPS_SHARE_MOUNT_ROOT/msquic

    exit 1
fi
