#!/bin/bash
# P25: list suffix, nursery arm, several worker settings, one run each. usage: limit-test.sh <frame>
F=$1
S=/root/p25/scripts/sweep-threads.sh
$S $F listsuffix lxr-nursery "4" 4
$S $F listsuffix lxr-nursery "4" 1
$S $F listsuffix lxr-nursery "13" 13
$S $F listsuffix lxr-nursery "13" 3
$S $F listsuffix lxr-nursery "13" 13
