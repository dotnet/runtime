#!/usr/bin/env bash

start_time=$(date +%s)
triggered="false"

while true; do
  echo "--------vm stats-----"
  echo " for $(date)"
  echo "--------disk---------"
  df -h
  echo "--------memory-------"
  free -h
  echo "--------processes----"
  ps -aux
  echo "--------done---------"
  sleep 5

  current_time=$(date +%s)
  elapsed_time=$(($current_time - $start_time))
  echo "-------- elapsed_time $elapsed_time ---------"

  if [ $elapsed_time -ge 180 ] && [ "$triggered" == "false" ]; then
    triggered="true"
    echo "--------installing---------"
    sudo /__w/1/s/.dotnet/dotnet tool install --global dotnet-trace
    export pid=$(ps aux | grep "noautoresponse" | sort -nrk 4 | awk 'NR==1{print $2}')
    echo "--------collecting $pid ---------"
    /__w/1/s/.dotnet/tools/dotnet-trace collect --profile gc-collect -p "$pid" --duration 00:01:00 --output $CurrentRepoSourceBuildArtifactsPackagesDir/trace.nettrace
    echo "--------end collecting---------"
  fi
done
