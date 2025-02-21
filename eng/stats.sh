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

  if [ $elapsed_time -ge 600 ] && [ "$triggered" == "false" ]; then
    triggered="true"
    echo "--------installing---------"
    sudo /__w/1/s/.dotnet/dotnet tool install --global dotnet-trace
    export pid=$(ps aux | grep "noautoresponse" | sort -nrk 4 | awk 'NR==1{print $2}')
    echo "--------collecting $pid  ---------"
    mkdir -p /__w/1/s/artifacts/log/
    mkdir -p /__w/1/a/artifacts/log/
    ls -la /__w/1/s/artifacts/log/
    ls -la /__w/1/a/artifacts/log/
    timestamp=$(date +%s)
    /root/.dotnet/tools/dotnet-trace collect --profile gc-collect -p "$pid" --duration 00:10:00 --output /__w/1/s/artifacts/log/trace.$timestamp.nettrace
    ls -la /mnt/vss/_work/1/a/artifacts/log/
    cp /__w/1/s/artifacts/log/trace.$timestamp.nettrace /__w/1/a/artifacts/log/trace.$timestamp.nettrace
    echo "--------end collecting---------"
  fi
done
