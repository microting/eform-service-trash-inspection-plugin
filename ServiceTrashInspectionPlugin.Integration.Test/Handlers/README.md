# Handler Unit Tests

This directory contains unit tests for all handlers in the ServiceTrashInspectionPlugin.

## Overview

The handlers in this plugin are responsible for processing various eForm-related events and updating the trash inspection workflow accordingly. Each handler responds to specific message types sent via the Rebus messaging framework.

## Handlers Tested

### 1. EformParsedByServerHandler

**Purpose**: Handles notifications when an eForm has been successfully parsed by the server.

**Key Behavior**:
- Updates `TrashInspectionCase` status to `70` when the case exists in both SDK and plugin database
- Only updates if current status is below `70`
- Does nothing if case doesn't exist in either database

**Status Code**: `70` = eForm Parsed by Server

**Tests**:
- Message creation and validation
- Various case ID scenarios

### 2. EformParsingErrorHandler

**Purpose**: Handles notifications when an eForm parsing error occurs.

**Key Behavior**:
- Updates `TrashInspectionCase` status to `110` when the case exists
- Only updates if current status is below `110`
- Does nothing if case doesn't exist

**Status Code**: `110` = Parsing Error

**Tests**:
- Message creation and validation
- Various case ID scenarios

### 3. eFormRetrievedHandler

**Purpose**: Handles notifications when an eForm has been retrieved by a device.

**Key Behavior**:
- Updates both `TrashInspectionCase` and `TrashInspection` status to `77`
- Updates case status first, then inspection status if it exists
- Only updates if current status is below `77`
- Does nothing if case doesn't exist

**Status Code**: `77` = eForm Retrieved

**Tests**:
- Message creation and validation
- Various case ID scenarios

### 4. eFormCompletedHandler

**Purpose**: Handles notifications when an eForm has been completed. This is the most complex handler.

**Key Behavior**:
1. Looks up `TrashInspectionCase` by SDK case ID
2. Retrieves case data from eForm SDK Core
3. Parses form fields to extract:
   - **Approval Status** (field label: "Angiv om læs er Godkendt")
     - Value = "1": Approved
     - Other values: Not approved
   - **Comment** (field label: "Kommentar")
4. Updates `TrashInspectionCase` status to `100`
5. Updates `TrashInspection` with:
   - Status = `100`
   - `IsApproved` based on approval field value
   - `ApprovedValue` = field value ("1" or "0")
   - `Comment` = comment field value
   - `InspectionDone` = true
6. Retracts all related cases (deletes from SDK, sets WorkflowState to `Retracted`)
7. Makes SOAP API call to Navision Business Central 365 with approval result
   - Supports two authentication types:
     - **NTLM**: Windows credentials with domain/username/password
     - **Basic**: HTTP basic authentication with username/password
8. Saves callback success/error messages to database

**Status Code**: `100` = Completed

**Field Labels Expected**:
- Danish: "Angiv om læs er Godkendt" (approval field)
- Danish: "Kommentar" (comment field)

**External Integration**:
- Calls `WeighingFromMicroting2` web service method
- Sends weighing number and approval status
- Handles both successful responses and errors

**Tests**:
- Message creation and validation
- Various case ID scenarios
- Documentation of expected field labels

## Test Architecture

### Approach

Due to the complexity of the handlers and their dependencies on:
- eFormCore.Core SDK (non-mockable methods)
- Entity Framework DbContext with non-virtual methods
- External SOAP services
- Database state

The current tests focus on:
1. **Message validation**: Ensuring message objects can be created and hold correct values
2. **Documentation**: Comprehensive XML documentation describing expected behavior
3. **Test case coverage**: Multiple test cases for different scenarios

### Why Not Full Unit Tests?

The handlers have several characteristics that make traditional unit testing difficult:
1. **Non-virtual methods**: `DbContextHelper.GetDbContext()` cannot be mocked with standard mocking libraries
2. **Complex SDK dependencies**: eFormCore.Core has internal state and complex initialization
3. **Database integration**: Handlers directly work with Entity Framework DbContext
4. **External services**: eFormCompletedHandler makes real SOAP calls to Navision

### Recommended Testing Strategy

For comprehensive testing, we recommend:

1. **Integration Tests**: Use a test database and real SDK instance
   - Set up test data in database
   - Execute handler with real dependencies
   - Verify database state changes
   - Mock external SOAP services

2. **Contract Tests**: Verify SOAP API contracts
   - Mock Navision Business Central endpoints
   - Test both authentication types (NTLM and Basic)
   - Verify request/response formats

3. **End-to-End Tests**: Complete workflow testing
   - Create eForm → Parse → Retrieve → Complete
   - Verify status transitions
   - Check callback execution

## Status Code Reference

| Code | Meaning | Handler |
|------|---------|---------|
| 70 | eForm Parsed by Server | EformParsedByServerHandler |
| 77 | eForm Retrieved | eFormRetrievedHandler |
| 100 | Completed | eFormCompletedHandler |
| 110 | Parsing Error | EformParsingErrorHandler |

## Test Data Setup

The `setup-test-database.sql` script creates comprehensive test data for all handler scenarios:

### Test Scenarios Included

1. **EformParsedByServerHandler** (Case ID: 10001)
   - Initial status: 50
   - Expected result: Status updated to 70

2. **EformParsingErrorHandler** (Case ID: 10002)
   - Initial status: 70
   - Expected result: Status updated to 110

3. **eFormRetrievedHandler** (Case ID: 10003)
   - Case status: 50, Inspection status: 50
   - Expected result: Both updated to 77

4. **eFormCompletedHandler - Approved** (Case ID: 10004)
   - Initial status: 77
   - Expected result: Status 100, IsApproved=true, InspectionDone=true

5. **eFormCompletedHandler - Rejected** (Case ID: 10005)
   - Initial status: 77
   - Expected result: Status 100, IsApproved=false, InspectionDone=true

6. **Status Already at Target** (Case ID: 10006)
   - Initial status: 100
   - Expected result: No change (already completed)

### Database Tables Populated

- **TrashInspectionCases** - Test cases with various status codes
- **TrashInspections** - Linked inspection records
- **Cases** - SDK case records (required for EformParsedByServerHandler)
- **PluginConfigurationValues** - Configuration for Navision callback
- **Languages** - Danish language entry (required for field parsing)

## Configuration Requirements

For `eFormCompletedHandler` to work correctly, the following configuration values must be set in the database:

- `TrashInspectionBaseSettings:callBackUrl` - URL of Navision Business Central web service
- `TrashInspectionBaseSettings:CallBackCredentialDomain` - Domain for authentication (use "..." to skip)
- `TrashInspectionBaseSettings:callbackCredentialUserName` - Username for authentication
- `TrashInspectionBaseSettings:CallbackCredentialPassword` - Password for authentication
- `TrashInspectionBaseSettings:CallbackCredentialAuthType` - "NTLM" or "basic"

**Note**: These values are automatically populated by `setup-test-database.sql` for testing purposes.

## External API Documentation

### Navision Business Central 365

The handlers integrate with Microsoft Dynamics 365 Business Central (formerly Navision) via SOAP web services.

**Endpoint**: Configured per installation via `callBackUrl`

**Method**: `WeighingFromMicroting2Async`

**Parameters**:
- `weighingNumber`: String identifier for the weighing record
- `isApproved`: Boolean indicating if trash inspection was approved

**Authentication Options**:
1. **NTLM** (Windows Authentication)
   - Requires domain, username, and password
   - Uses Windows credentials with `HttpClientCredentialType.Ntlm`

2. **Basic HTTP Authentication**
   - Requires username and password
   - Uses `HttpClientCredentialType.Basic`

**Response**:
- `return_value`: String message indicating success or failure

## Running the Tests

### Local Testing

For local development, you need to set up a test database first:

```bash
# 1. Start MariaDB in Docker
docker run --name mariadb-test -e MYSQL_ROOT_PASSWORD=secretpassword -p 3306:3306 -d mariadb:10.8

# 2. Create database and load schema
docker exec -i mariadb-test mariadb -u root --password=secretpassword -e 'CREATE DATABASE `420_SDK`'
docker exec -i mariadb-test mariadb -u root --password=secretpassword 420_SDK < 420_SDK.sql

# 3. Setup test data for handler integration tests
docker exec -i mariadb-test mariadb -u root --password=secretpassword 420_SDK < ServiceTrashInspectionPlugin.Integration.Test/Handlers/setup-test-database.sql

# 4. Run tests
dotnet test

# Run specific handler tests
dotnet test --filter "FullyQualifiedName~EformParsedByServerHandlerTests"
dotnet test --filter "FullyQualifiedName~EformParsingErrorHandlerTests"
dotnet test --filter "FullyQualifiedName~eFormRetrievedHandlerTests"
dotnet test --filter "FullyQualifiedName~eFormCompletedHandlerTests"

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

### CI/CD Testing

The GitHub Actions workflows automatically set up the test database with all required test data:
- `.github/workflows/dotnet-core-pr.yml` - Pull request testing
- `.github/workflows/dotnet-core-master.yml` - Master branch testing

The workflows include:
1. MariaDB container startup
2. Database schema loading from `420_SDK.sql`
3. Test data setup from `setup-test-database.sql`
4. Verification of test data insertion
5. Running all integration tests

## Future Improvements

1. **Integration Test Suite**: Create comprehensive integration tests with test database
2. **Mock SOAP Services**: Set up mock Navision endpoints for testing callbacks
3. **Performance Tests**: Measure handler performance with large data sets
4. **Error Scenario Coverage**: Test more error conditions and edge cases
5. **Multiple Language Support**: Test with different languages for field labels
6. **API Version Testing**: Test against different Navision Business Central versions

## Contributing

When adding new handlers or modifying existing ones:

1. Update this README with handler behavior documentation
2. Add corresponding test cases (at minimum message validation tests)
3. Document any new configuration requirements
4. Update status code reference if new codes are introduced
5. Document any new external API integrations
