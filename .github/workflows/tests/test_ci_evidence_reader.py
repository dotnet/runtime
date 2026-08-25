import importlib.machinery
import importlib.util
import io
import os
import sys
import tempfile
import unittest
import urllib.error
from pathlib import Path
from unittest import mock


SCRIPT = Path(__file__).parents[1] / "ci-evidence-reader"
LOADER = importlib.machinery.SourceFileLoader("ci_evidence_reader", str(SCRIPT))
SPEC = importlib.util.spec_from_loader(LOADER.name, LOADER)
ci_evidence_reader = importlib.util.module_from_spec(SPEC)
LOADER.exec_module(ci_evidence_reader)


class FakeResponse:
    def __init__(self, data=b"ok", status=200, content_length=None):
        self._stream = io.BytesIO(data)
        self.status = status
        self.headers = {}
        if content_length is not None:
            self.headers["Content-Length"] = str(content_length)

    def __enter__(self):
        return self

    def __exit__(self, *_):
        return False

    def read(self, size):
        return self._stream.read(size)


class FakeOpener:
    def __init__(self, response=None, error=None):
        self.response = response
        self.error = error
        self.requests = []

    def open(self, request, timeout):
        self.requests.append((request, timeout))
        if self.error:
            raise self.error
        return self.response


class UrlValidationTests(unittest.TestCase):
    def test_accepts_reader_azdo_build_list(self):
        url = ci_evidence_reader._azdo_builds_url(154, 25, 20)
        ci_evidence_reader._validate_url(url, {"azdo"})
        self.assertIn(
            "definitions=154&branchName=refs/heads/main&statusFilter=completed"
            "&resultFilter=succeeded,failed,partiallySucceeded&%24top=25&%24skip=20"
            "&api-version=7.1",
            url,
        )

    def test_accepts_any_positive_definition(self):
        url = ci_evidence_reader._azdo_builds_url(999)
        ci_evidence_reader._validate_url(url, {"azdo"})

    def test_rejects_invalid_top_and_skip(self):
        for top in (1, 24, 26, 100):
            with self.assertRaises(ci_evidence_reader.TransportError):
                ci_evidence_reader._validate_url(
                    ci_evidence_reader._azdo_builds_url(154, top), {"azdo"}
                )
        for skip in (1, 9, 50):
            with self.assertRaises(ci_evidence_reader.TransportError):
                ci_evidence_reader._validate_url(
                    ci_evidence_reader._azdo_builds_url(154, 25, skip), {"azdo"}
                )

    def test_rejects_credentials_and_arbitrary_hosts(self):
        for url in (
            "https://user:password@dev.azure.com/dnceng-public/public/_apis/build/builds",
            "https://example.com/",
        ):
            with self.subTest(url=url):
                with self.assertRaises(ci_evidence_reader.TransportError):
                    ci_evidence_reader._validate_url(url, {"azdo"})

    def test_accepts_only_helix_console_blobs(self):
        valid = (
            "https://helixre107v0xdeko0k025g8.blob.core.windows.net/"
            "dotnet-runtime-refs-heads-main/job/1/console.1234.log"
            "?sv=2020-01-01&sr=c&sig=signature&se=2030-01-01&sp=rl"
        )
        ci_evidence_reader._validate_url(valid, {"helix-console"})
        with self.assertRaises(ci_evidence_reader.TransportError):
            ci_evidence_reader._validate_url(
                "https://other.blob.core.windows.net/container/secrets.txt",
                {"helix-console"},
            )

    def test_rejects_undocumented_blob_query_parameter(self):
        url = (
            "https://helixre107v0xdeko0k025g8.blob.core.windows.net/"
            "dotnet-runtime-refs-heads-main/job/1/console.1234.log?sk=value"
        )
        with self.assertRaisesRegex(
            ci_evidence_reader.TransportError, "unexpected Helix console blob"
        ):
            ci_evidence_reader._validate_url(url, {"helix-console"})

    def test_helix_work_items_use_specific_family(self):
        url = ci_evidence_reader._helix_work_items_url(
            "00000000-0000-0000-0000-000000000000"
        )
        ci_evidence_reader._validate_url(url, {"helix-work-items"})

    def test_redirect_handler_rejects_family_escape(self):
        handler = ci_evidence_reader._ValidatingRedirectHandler({"azdo"})
        with self.assertRaises(ci_evidence_reader.TransportError):
            handler.redirect_request(
                None,
                None,
                302,
                "Found",
                {},
                "https://helix.dot.net/api/jobs/"
                "00000000-0000-0000-0000-000000000000/workitems"
                "?api-version=2019-06-17",
            )


class OutputPathTests(unittest.TestCase):
    def setUp(self):
        self.original_root = ci_evidence_reader.OUTPUT_ROOT
        self.temp = tempfile.TemporaryDirectory()
        ci_evidence_reader.OUTPUT_ROOT = Path(self.temp.name)

    def tearDown(self):
        ci_evidence_reader.OUTPUT_ROOT = self.original_root
        self.temp.cleanup()

    def test_accepts_output_below_root(self):
        output = Path(self.temp.name) / "metadata" / "builds.json"
        self.assertEqual(
            ci_evidence_reader._validate_output_path(str(output), (".json",)),
            output.resolve(),
        )

    def test_rejects_output_outside_root_and_wrong_suffix(self):
        cases = (
            (str(Path(self.temp.name).parent / "outside.json"), (".json",)),
            (str(Path(self.temp.name) / "file.tsv"), (".json",)),
        )
        for output, suffixes in cases:
            with self.subTest(output=output):
                with self.assertRaises(ci_evidence_reader.TransportError):
                    ci_evidence_reader._validate_output_path(output, suffixes)

    def test_rejects_existing_directory(self):
        output = Path(self.temp.name) / "directory.json"
        output.mkdir()
        with self.assertRaisesRegex(
            ci_evidence_reader.TransportError, "regular file"
        ):
            ci_evidence_reader._validate_output_path(str(output), (".json",))

    def test_rejects_symlink_parent_outside_root(self):
        symlink_parent = Path(self.temp.name) / "symlink-parent"
        symlink_parent.symlink_to(
            Path(self.temp.name).parent, target_is_directory=True
        )

        with self.assertRaisesRegex(
            ci_evidence_reader.TransportError, "output path must be under"
        ):
            ci_evidence_reader._validate_output_path(
                str(symlink_parent / "output.json"), (".json",)
            )

    def test_rejects_symlink_parent_during_write(self):
        real_parent = Path(self.temp.name) / "real-parent"
        real_parent.mkdir()
        symlink_parent = Path(self.temp.name) / "symlink-parent"
        symlink_parent.symlink_to(real_parent, target_is_directory=True)

        with self.assertRaisesRegex(
            ci_evidence_reader.TransportError, "real directory"
        ):
            ci_evidence_reader._write_output(
                b"data", str(symlink_parent / "output.json"), (".json",)
            )


class RequestBehaviorTests(unittest.TestCase):
    def setUp(self):
        self.url = ci_evidence_reader._azdo_builds_url(154)

    def test_get_only_with_fixed_timeout_and_user_agent(self):
        opener = FakeOpener(FakeResponse(b"{}"))
        self.assertEqual(
            ci_evidence_reader._request_bytes(
                self.url, {"azdo"}, ci_evidence_reader.JSON_LIMIT, opener
            ),
            b"{}",
        )
        request, timeout = opener.requests[0]
        self.assertEqual(request.get_method(), "GET")
        self.assertEqual(timeout, ci_evidence_reader.TIMEOUT_SECONDS)
        self.assertEqual(request.get_header("User-agent"), ci_evidence_reader.USER_AGENT)

    def test_rejects_content_length_and_stream_over_limit(self):
        for response in (
            FakeResponse(b"small", content_length=11),
            FakeResponse(b"01234567890"),
        ):
            with self.subTest(response=response):
                with self.assertRaises(ci_evidence_reader.TransportError):
                    ci_evidence_reader._request_bytes(
                        self.url, {"azdo"}, 10, FakeOpener(response)
                    )

    def test_surfaces_http_errors(self):
        error = urllib.error.HTTPError(self.url, 503, "Unavailable", {}, None)
        with self.assertRaisesRegex(ci_evidence_reader.TransportError, "status 503"):
            ci_evidence_reader._request_bytes(
                self.url, {"azdo"}, 10, FakeOpener(error=error)
            )


class CommandDispatchTests(unittest.TestCase):
    def test_direct_commands_use_their_request_specs(self):
        cases = (
            (
                ["azdo-builds", "--definition", "999", "--output", "/tmp/builds.json"],
                ci_evidence_reader._azdo_builds_url(999),
                {"azdo"},
                ci_evidence_reader.JSON_LIMIT,
                (".json",),
            ),
            (
                [
                    "azdo-timeline",
                    "--build-id",
                    "123",
                    "--output",
                    "/tmp/timeline.json",
                ],
                ci_evidence_reader._azdo_timeline_url(123),
                {"azdo"},
                ci_evidence_reader.JSON_LIMIT,
                (".json",),
            ),
            (
                [
                    "azdo-log",
                    "--build-id",
                    "123",
                    "--log-id",
                    "456",
                    "--output",
                    "/tmp/log.log",
                ],
                ci_evidence_reader._azdo_log_url(123, 456),
                {"azdo"},
                ci_evidence_reader.LOG_LIMIT,
                (".log", ".txt"),
            ),
            (
                [
                    "helix-work-items",
                    "--job-id",
                    "00000000-0000-0000-0000-000000000000",
                    "--output",
                    "/tmp/work-items.json",
                ],
                ci_evidence_reader._helix_work_items_url(
                    "00000000-0000-0000-0000-000000000000"
                ),
                {"helix-work-items"},
                ci_evidence_reader.JSON_LIMIT,
                (".json",),
            ),
        )

        for command, url, families, limit, suffixes in cases:
            with self.subTest(command=command[0]):
                args = ci_evidence_reader._parser().parse_args(command)
                with (
                    mock.patch.object(
                        ci_evidence_reader, "_request_bytes", return_value=b"payload"
                    ) as request,
                    mock.patch.object(ci_evidence_reader, "_write_output") as write,
                ):
                    ci_evidence_reader._run(args)
                request.assert_called_once_with(url, families, limit)
                write.assert_called_once_with(b"payload", args.output, suffixes)

    def test_helix_console_resolves_then_reads_console(self):
        job_id = "00000000-0000-0000-0000-000000000000"
        args = ci_evidence_reader._parser().parse_args(
            [
                "helix-console",
                "--job-id",
                job_id,
                "--work-item",
                "runtime-tests",
                "--output",
                "/tmp/console.log",
            ]
        )
        console_url = (
            "https://helixre107v0xdeko0k025g8.blob.core.windows.net/"
            "dotnet-runtime/job/console.1.log?helixlogtype=result"
        )
        with (
            mock.patch.object(
                ci_evidence_reader,
                "_request_bytes",
                side_effect=[b"work-items", b"console"],
            ) as request,
            mock.patch.object(
                ci_evidence_reader, "_console_url", return_value=console_url
            ) as resolve_console,
            mock.patch.object(ci_evidence_reader, "_write_output") as write,
        ):
            ci_evidence_reader._run(args)

        request.assert_has_calls(
            [
                mock.call(
                    ci_evidence_reader._helix_work_items_url(job_id),
                    {"helix-work-items"},
                    ci_evidence_reader.JSON_LIMIT,
                ),
                mock.call(
                    console_url, {"helix-console"}, ci_evidence_reader.LOG_LIMIT
                ),
            ]
        )
        self.assertEqual(request.call_count, 2)
        resolve_console.assert_called_once_with(b"work-items", "runtime-tests")
        write.assert_called_once_with(
            b"console", args.output, (".log", ".txt")
        )


class HelixTraversalTests(unittest.TestCase):
    def test_console_url_is_selected_by_exact_work_item_name(self):
        payload = b"""[
          {
            "Name": "runtime-tests",
            "ConsoleOutputUri": "https://helixre107v0xdeko0k025g8.blob.core.windows.net/dotnet-runtime/job/console.1.log?helixlogtype=result"
          }
        ]"""
        self.assertIn(
            "console.1.log",
            ci_evidence_reader._console_url(payload, "runtime-tests"),
        )

    def test_console_url_rejects_untrusted_metadata(self):
        payload = b"""[
          {
            "Name": "runtime-tests",
            "ConsoleOutputUri": "https://example.com/console.log"
          }
        ]"""
        with self.assertRaises(ci_evidence_reader.TransportError):
            ci_evidence_reader._console_url(payload, "runtime-tests")

if __name__ == "__main__":
    unittest.main()