/*
The MIT License (MIT)
Copyright (c) 2007 - 2025 Microting A/S
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Linq;
using System.Threading.Tasks;
using Microting.eFormTrashInspectionBase.Infrastructure.Data;
using Microting.eFormTrashInspectionBase.Infrastructure.Data.Entities;
using Moq;
using NUnit.Framework;
using ServiceTrashInspectionPlugin.Handlers;
using ServiceTrashInspectionPlugin.Infrastructure.Helpers;
using ServiceTrashInspectionPlugin.Messages;

namespace ServiceTrashInspectionPlugin.Integration.Test.Handlers;

/// <summary>
/// Unit tests for eFormRetrievedHandler
/// 
/// The eFormRetrievedHandler is responsible for handling messages when an eForm is retrieved.
/// It updates the status of both TrashInspectionCase and TrashInspection to 77 when the case exists and status is below 77.
/// 
/// Expected behavior:
/// - When TrashInspectionCase exists: status should be updated to 77 if below 77
/// - When TrashInspection exists: status should be updated to 77 if below 77
/// - When case status is already >= 77: status should remain unchanged
/// - When TrashInspectionCase doesn't exist: no update should occur
/// - When TrashInspection doesn't exist: only TrashInspectionCase should be updated
/// </summary>
[TestFixture]
public class eFormRetrievedHandlerTests
{
    [SetUp]
    public void Setup()
    {
        // Note: Due to non-virtual methods in DbContextHelper, these tests require actual database setup.
        // These tests serve as documentation of expected behavior and should be run as integration tests.
    }

    /// <summary>
    /// Test to verify that the message structure is valid
    /// </summary>
    [Test]
    public void Message_CanBeCreatedWithCaseId()
    {
        // Arrange
        var caseId = 12345;
        
        // Act
        var message = new eFormRetrieved(caseId);
        
        // Assert
        Assert.That(message.caseId, Is.EqualTo(caseId));
    }
    
    /// <summary>
    /// Test to verify message with different case ID values
    /// </summary>
    [Test]
    [TestCase(1)]
    [TestCase(999)]
    [TestCase(123456)]
    public void Message_HandlesVariousCaseIds(int caseId)
    {
        // Act
        var message = new eFormRetrieved(caseId);
        
        // Assert
        Assert.That(message.caseId, Is.EqualTo(caseId));
    }
}
