# Enterprise Scenario Testing

Detailed instructions for running these tests is located here:

src\libraries\Common\tests\System\Net\EnterpriseTests\setup\README.md

## FTP/SSL Tests

These tests validate FTP operations with SSL/TLS (FTPS) against a ProFTPD server configured in the enterprise test environment.

The tests cover:
- FTP file upload with explicit SSL/TLS
- FTP file download with explicit SSL/TLS
- Proper SSL/TLS stream closure to prevent protocol violations
