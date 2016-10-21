#!/bin/sh
for f in *.patch; do grep $f ../*.py > /dev/null || echo $f; done
for f in */*.patch; do grep $f ../*.py > /dev/null || echo $f; done
