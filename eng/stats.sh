#!/usr/bin/env bash

/tmp/docker exec -t -u root sample tdnf install -y sudo

sudo tdnf install -y procps-ng

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
done
