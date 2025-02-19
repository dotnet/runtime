#!/usr/bin/env bash

start_time=$(date +%s)

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
  elapsed_time=$((current_time - start_time))

  if [ $elapsed_time -ge 1200 ] && [ "$triggered" = false ]; then
    triggered=true
    sudo dotnet tool install --global dotnet-trace
    export pid=$(ps aux | grep "dotnet" | sort -nrk 4 | awk 'NR==1{print $2}')
    echo "--------collecting---------"
    dotnet-trace collect --profile gc-collect -p "$msb" --duration 00:20:00 --output $CurrentRepoSourceBuildArtifactsPackagesDir/trace.nettrace
    echo "--------end collecting---------"
  fi
done
