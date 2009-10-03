# Postprocessed with patch-quiet.sh
RUNTIME = mono --debug
CAS_RUNTIME = $(RUNTIME) --security
CSCOMPILE = gmcs -debug
PROFILE = net_2_0

all:	sandbox.exe whoami.exe quota.exe

aot:	sandbox.exe.so whoami.exe.so quota.exe.so

TEST_FILES = whoami.exe sandbox.exe "sandbox.exe whoami.exe"
TEST_POLICIES = @Fulltrust @LocalIntranet @Internet @SkipVerification @Execution @Nothing @Everything

run: all
	@for p in $(TEST_POLICIES); do	\
		for i in $(TEST_FILES); do	\
			echo -e "TESTING: $(RUNTIME) sandbox.exe $$p $$i";	\
			$(CAS_RUNTIME) sandbox.exe $$p $$i;	\
		done; \
	done;

test: run
# todo

clean:
	rm -f *.exe* *.dll* *.snk

%.exe: %.cs
	$(CSCOMPILE) $^ /out:$@

makepol.exe: makepol.cs
	$(CSCOMPILE) $^ /out:$@ -r:System.dll -r:System.Drawing.dll

%.exe.so: %.exe
	$(RUNTIME) --aot $^
