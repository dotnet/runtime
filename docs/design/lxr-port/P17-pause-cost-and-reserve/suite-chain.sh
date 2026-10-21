set -u
# usage: suite-chain.sh <label> <repo-wsl-path>   native Release+Debug, then managed, all from the given source tree
LABEL=$1
SRC=$2
cd /root/p17
bash scripts/native-linux.sh $SRC Release /root/p17/native/$LABEL-Release > native-$LABEL-Release.txt 2>&1
bash scripts/native-linux.sh $SRC Debug /root/p17/native/$LABEL-Debug > native-$LABEL-Debug.txt 2>&1
python3 scripts/run-managed.py --frames Release=/root/p17/frames/$LABEL-Release-linux,Debug=/root/p17/frames/$LABEL-Debug-linux --modules Release=/root/p17/frames/$LABEL-Release-linux/libclrlxrgc.so,Debug=/root/p17/frames/$LABEL-Debug-linux/libclrlxrgc.so --fixtures /root/lxr-batching-d2eccaea --output /root/p17/managed/$LABEL > managed-$LABEL.txt 2>&1
echo "chain $LABEL done"
