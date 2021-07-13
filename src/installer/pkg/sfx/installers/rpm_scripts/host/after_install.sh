#!/usr/bin/env bash

if [[ -L /usr/local/bin/dotnet ]]; then
  rm /usr/local/bin/dotnet
fi
ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet

