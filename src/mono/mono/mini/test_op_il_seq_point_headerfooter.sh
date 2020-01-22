#!/bin/sh -e

TESTRESULT_FILE=TestResult-op_il_seq_point.tmp
TOTAL=$(grep -c "<test-case" $TESTRESULT_FILE || true)
FAILURES=$(grep -c "<failure>" $TESTRESULT_FILE || true)
if [ "$FAILURES" -eq "0" ] && [ "$TOTAL" -ne "0" ]
then
	PASS="True"
else
	PASS="False"
fi
MYLOCALE=$(echo $LANG | cut -f1 -d'.')
MYUNAME=$(uname -r)
MYHOSTNAME=$(hostname -s)
MYFQDN=$(hostname -f)
MYDATE=$(date +%F)
MYTIME=$(date +%T)

echo "            </results>" >> $TESTRESULT_FILE
echo "          </test-suite>" >> $TESTRESULT_FILE
echo "        </results>" >> $TESTRESULT_FILE
echo "      </test-suite>" >> $TESTRESULT_FILE
echo "    </results>" >> $TESTRESULT_FILE
echo "  </test-suite>" >> $TESTRESULT_FILE
echo "</test-results>" >> $TESTRESULT_FILE

echo "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?>" > $TESTRESULT_FILE.header
echo "<!--This file represents the results of running a test suite-->" >> $TESTRESULT_FILE.header
echo "<test-results name=\"regression-tests.dummy\" total=\"${TOTAL}\" failures=\"${FAILURES}\" not-run=\"0\" date=\"${MYDATE}\" time=\"${MYTIME}\">" >> $TESTRESULT_FILE.header
echo "  <environment nunit-version=\"2.4.8.0\" clr-version=\"4.0.30319.17020\" os-version=\"Unix ${MYUNAME}\" platform=\"Unix\" cwd=\"${PWD}\" machine-name=\"${MYHOSTNAME}\" user=\"${USER}\" user-domain=\"${MYFQDN}\" />" >> $TESTRESULT_FILE.header
echo "  <culture-info current-culture=\"${MYLOCALE}\" current-uiculture=\"${MYLOCALE}\" />" >> $TESTRESULT_FILE.header
echo "  <test-suite name=\"op_il_seq_point-tests.dummy\" success=\"${PASS}\" time=\"0\" asserts=\"0\">" >> $TESTRESULT_FILE.header
echo "    <results>" >> $TESTRESULT_FILE.header
echo "      <test-suite name=\"MonoTests\" success=\"${PASS}\" time=\"0\" asserts=\"0\">" >> $TESTRESULT_FILE.header
echo "        <results>" >> $TESTRESULT_FILE.header
echo "          <test-suite name=\"op_il_seq_point\" success=\"${PASS}\" time=\"0\" asserts=\"0\">" >> $TESTRESULT_FILE.header
echo "            <results>" >> $TESTRESULT_FILE.header

cat $TESTRESULT_FILE.header $TESTRESULT_FILE > $(basename $TESTRESULT_FILE .tmp).xml
rm -f $TESTRESULT_FILE.header $TESTRESULT_FILE
