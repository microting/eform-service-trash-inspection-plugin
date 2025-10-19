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

## Configuration Requirements

For `eFormCompletedHandler` to work correctly, the following configuration values must be set in the database:

- `TrashInspectionBaseSettings:callBackUrl` - URL of Navision Business Central web service
- `TrashInspectionBaseSettings:CallBackCredentialDomain` - Domain for authentication (use "..." to skip)
- `TrashInspectionBaseSettings:callbackCredentialUserName` - Username for authentication
- `TrashInspectionBaseSettings:CallbackCredentialPassword` - Password for authentication
- `TrashInspectionBaseSettings:CallbackCredentialAuthType` - "NTLM" or "basic"

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

```bash
# Run all tests
dotnet test

# Run specific handler tests
dotnet test --filter "FullyQualifiedName~EformParsedByServerHandlerTests"
dotnet test --filter "FullyQualifiedName~EformParsingErrorHandlerTests"
dotnet test --filter "FullyQualifiedName~eFormRetrievedHandlerTests"
dotnet test --filter "FullyQualifiedName~eFormCompletedHandlerTests"

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

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
