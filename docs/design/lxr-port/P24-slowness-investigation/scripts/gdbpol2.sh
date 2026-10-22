#!/bin/bash
/root/p24/scripts/gdb-policy.sh listsuffix lxr-nursery /root/p24/results/policy-listsuffix-nursery.json | tail -1
/root/p24/scripts/gdb-policy.sh binarytrees lxr-nonmoving /root/p24/results/policy-binarytrees-nonmoving.json | tail -1
