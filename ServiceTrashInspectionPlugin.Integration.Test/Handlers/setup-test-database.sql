-- Setup test database for handler integration tests
-- This script creates the necessary test data for all handler test scenarios
-- as described in ServiceTrashInspectionPlugin.Integration.Test/Handlers/README.md

USE `420_SDK`;

-- Create PluginConfigurationValues table if it doesn't exist
-- This table is used by eFormApi.BasePn for plugin configuration
CREATE TABLE IF NOT EXISTS `PluginConfigurationValues` (
    `Id` int(11) NOT NULL AUTO_INCREMENT,
    `Name` varchar(255) NOT NULL,
    `Value` longtext DEFAULT NULL,
    `CreatedAt` datetime(6) DEFAULT NULL,
    `UpdatedAt` datetime(6) DEFAULT NULL,
    `WorkflowState` varchar(255) DEFAULT NULL,
    `Version` int(11) DEFAULT NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Create TrashInspectionCases table if it doesn't exist
CREATE TABLE IF NOT EXISTS `TrashInspectionCases` (
    `Id` int(11) NOT NULL AUTO_INCREMENT,
    `SdkCaseId` varchar(255) DEFAULT NULL,
    `Status` int(11) DEFAULT NULL,
    `TrashInspectionId` int(11) DEFAULT NULL,
    `CreatedAt` datetime(6) DEFAULT NULL,
    `UpdatedAt` datetime(6) DEFAULT NULL,
    `WorkflowState` varchar(255) DEFAULT NULL,
    `Version` int(11) DEFAULT NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Create TrashInspections table if it doesn't exist
CREATE TABLE IF NOT EXISTS `TrashInspections` (
    `Id` int(11) NOT NULL AUTO_INCREMENT,
    `Status` int(11) DEFAULT NULL,
    `WeighingNumber` varchar(255) DEFAULT NULL,
    `IsApproved` tinyint(1) DEFAULT 0,
    `Comment` longtext DEFAULT NULL,
    `ApprovedValue` varchar(255) DEFAULT NULL,
    `InspectionDone` tinyint(1) DEFAULT 0,
    `ResponseSendToCallBackUrl` tinyint(1) DEFAULT 0,
    `SuccessMessageFromCallBack` longtext DEFAULT NULL,
    `ErrorFromCallBack` longtext DEFAULT NULL,
    `CreatedAt` datetime(6) DEFAULT NULL,
    `UpdatedAt` datetime(6) DEFAULT NULL,
    `WorkflowState` varchar(255) DEFAULT NULL,
    `Version` int(11) DEFAULT NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Insert configuration values for eFormCompletedHandler
-- These are needed for the SOAP callback to Navision Business Central 365
INSERT INTO `PluginConfigurationValues` (`Name`, `Value`, `CreatedAt`, `UpdatedAt`, `WorkflowState`, `Version`)
VALUES 
    ('TrashInspectionBaseSettings:callBackUrl', 'http://mock-navision-service/WeighingService', NOW(), NOW(), 'Created', 1),
    ('TrashInspectionBaseSettings:CallBackCredentialDomain', 'TESTDOMAIN', NOW(), NOW(), 'Created', 1),
    ('TrashInspectionBaseSettings:callbackCredentialUserName', 'testuser', NOW(), NOW(), 'Created', 1),
    ('TrashInspectionBaseSettings:CallbackCredentialPassword', 'testpassword', NOW(), NOW(), 'Created', 1),
    ('TrashInspectionBaseSettings:CallbackCredentialAuthType', 'basic', NOW(), NOW(), 'Created', 1)
ON DUPLICATE KEY UPDATE 
    `Value` = VALUES(`Value`),
    `UpdatedAt` = NOW();

-- Test Scenario 1: EformParsedByServerHandler - Case exists with status below 70
-- Expected: Status should be updated to 70
INSERT INTO `TrashInspectionCases` (`Id`, `SdkCaseId`, `Status`, `TrashInspectionId`, `CreatedAt`, `UpdatedAt`, `WorkflowState`, `Version`)
VALUES 
    (1, '10001', 50, 1, NOW(), NOW(), 'Created', 1);

-- Test Scenario 2: EformParsingErrorHandler - Case exists with status below 110
-- Expected: Status should be updated to 110
INSERT INTO `TrashInspectionCases` (`Id`, `SdkCaseId`, `Status`, `TrashInspectionId`, `CreatedAt`, `UpdatedAt`, `WorkflowState`, `Version`)
VALUES 
    (2, '10002', 70, 2, NOW(), NOW(), 'Created', 1);

-- Test Scenario 3: eFormRetrievedHandler - Case exists with status below 77, with linked inspection
-- Expected: Both case status and inspection status should be updated to 77
INSERT INTO `TrashInspectionCases` (`Id`, `SdkCaseId`, `Status`, `TrashInspectionId`, `CreatedAt`, `UpdatedAt`, `WorkflowState`, `Version`)
VALUES 
    (3, '10003', 50, 3, NOW(), NOW(), 'Created', 1);

INSERT INTO `TrashInspections` (`Id`, `Status`, `WeighingNumber`, `IsApproved`, `Comment`, `InspectionDone`, `CreatedAt`, `UpdatedAt`, `WorkflowState`, `Version`)
VALUES 
    (3, 50, 'W10003', 0, '', 0, NOW(), NOW(), 'Created', 1);

-- Test Scenario 4: eFormCompletedHandler - Complete workflow with approval
-- Expected: Status updated to 100, inspection marked as approved and done
INSERT INTO `TrashInspectionCases` (`Id`, `SdkCaseId`, `Status`, `TrashInspectionId`, `CreatedAt`, `UpdatedAt`, `WorkflowState`, `Version`)
VALUES 
    (4, '10004', 77, 4, NOW(), NOW(), 'Created', 1);

INSERT INTO `TrashInspections` (`Id`, `Status`, `WeighingNumber`, `IsApproved`, `Comment`, `InspectionDone`, `CreatedAt`, `UpdatedAt`, `WorkflowState`, `Version`)
VALUES 
    (4, 77, 'W10004', 0, '', 0, NOW(), NOW(), 'Created', 1);

-- Test Scenario 5: eFormCompletedHandler - Complete workflow with rejection
-- Expected: Status updated to 100, inspection marked as not approved
INSERT INTO `TrashInspectionCases` (`Id`, `SdkCaseId`, `Status`, `TrashInspectionId`, `CreatedAt`, `UpdatedAt`, `WorkflowState`, `Version`)
VALUES 
    (5, '10005', 77, 5, NOW(), NOW(), 'Created', 1);

INSERT INTO `TrashInspections` (`Id`, `Status`, `WeighingNumber`, `IsApproved`, `Comment`, `InspectionDone`, `CreatedAt`, `UpdatedAt`, `WorkflowState`, `Version`)
VALUES 
    (5, 77, 'W10005', 0, '', 0, NOW(), NOW(), 'Created', 1);

-- Test Scenario 6: Status already at target - should not be updated
-- For testing that handlers don't update when status is already at or above target
INSERT INTO `TrashInspectionCases` (`Id`, `SdkCaseId`, `Status`, `TrashInspectionId`, `CreatedAt`, `UpdatedAt`, `WorkflowState`, `Version`)
VALUES 
    (6, '10006', 100, 6, NOW(), NOW(), 'Created', 1);

INSERT INTO `TrashInspections` (`Id`, `Status`, `WeighingNumber`, `IsApproved`, `Comment`, `InspectionDone`, `CreatedAt`, `UpdatedAt`, `WorkflowState`, `Version`)
VALUES 
    (6, 100, 'W10006', 1, 'Already completed', 1, NOW(), NOW(), 'Created', 1);

-- Corresponding SDK Cases for eFormParsedByServerHandler
-- These need to exist in the SDK database for the handler to process
INSERT INTO `Cases` (`Id`, `MicrotingUid`, `CreatedAt`, `UpdatedAt`, `WorkflowState`, `Version`)
VALUES 
    (1, 10001, NOW(), NOW(), 'Created', 1),
    (2, 10002, NOW(), NOW(), 'Created', 1),
    (3, 10003, NOW(), NOW(), 'Created', 1),
    (4, 10004, NOW(), NOW(), 'Created', 1),
    (5, 10005, NOW(), NOW(), 'Created', 1),
    (6, 10006, NOW(), NOW(), 'Created', 1);

-- Add Language entry for Danish (required for eFormCompletedHandler)
INSERT INTO `Languages` (`Id`, `Name`, `LanguageCode`, `IsActive`, `CreatedAt`, `UpdatedAt`, `WorkflowState`, `Version`)
VALUES 
    (1, 'Danish', 'da', 1, NOW(), NOW(), 'Created', 1)
ON DUPLICATE KEY UPDATE 
    `Name` = VALUES(`Name`);

-- Status code reference:
-- 50  = Initial/Created
-- 70  = eForm Parsed by Server (EformParsedByServerHandler)
-- 77  = eForm Retrieved (eFormRetrievedHandler)
-- 100 = Completed (eFormCompletedHandler)
-- 110 = Parsing Error (EformParsingErrorHandler)
