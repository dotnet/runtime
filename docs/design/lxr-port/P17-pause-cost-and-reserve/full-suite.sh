set -u
# usage: full-suite.sh <label> <repo-wsl-path>  builds Release+Debug collectors, runs native, managed and legacy suites
LABEL=$1
SRC=$2
cd /root/p17
bash scripts/build-linux.sh $SRC Release $LABEL
bash scripts/build-linux.sh $SRC Debug $LABEL
bash scripts/suite-chain.sh $LABEL $SRC
bash scripts/legacy-linux.sh $SRC /root/p17/legacy/$LABEL
echo "full-suite $LABEL done"
