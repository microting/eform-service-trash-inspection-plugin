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
using System.Xml.Linq;
using NUnit.Framework;
using ServiceTrashInspectionPlugin.Infrastructure.Helpers;

namespace ServiceTrashInspectionPlugin.Integration.Test.Helpers;

/// <summary>
/// Regression tests for the NAV MicrotingWS SOAP envelope construction and response parsing
/// used by the WeighingFromMicroting2 callback (eFormCompletedHandler.CallUrlNtlmAuth).
/// </summary>
[TestFixture]
public class NavSoapTests
{
    private const string Ns = "urn:microsoft-dynamics-schemas/codeunit/MicrotingWS";

    [Test]
    public void BuildEnvelope_StartsWithXmlDeclaration()
    {
        string soap = NavSoap.BuildWeighingFromMicroting2Envelope("V250790", true);

        Assert.That(soap, Does.StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>"));
    }

    [Test]
    [TestCase(true, "true")]
    [TestCase(false, "false")]
    public void BuildEnvelope_SerializesWeighingNoAndApproved(bool approved, string expectedApproved)
    {
        string soap = NavSoap.BuildWeighingFromMicroting2Envelope("V250790", approved);

        XNamespace svc = Ns;
        XElement operation = XDocument.Parse(soap).Descendants(svc + "WeighingFromMicroting2").Single();

        Assert.Multiple(() =>
        {
            Assert.That(operation.Element(svc + "_WeighingNo")!.Value, Is.EqualTo("V250790"));
            Assert.That(operation.Element(svc + "_Approved")!.Value, Is.EqualTo(expectedApproved));
        });
    }

    [Test]
    public void BuildEnvelope_EscapesSpecialCharactersInWeighingNo()
    {
        // Would produce invalid XML (and XDocument.Parse would throw) if the value were not escaped.
        string soap = NavSoap.BuildWeighingFromMicroting2Envelope("A&B<C>", true);

        XNamespace svc = Ns;
        Assert.That(XDocument.Parse(soap).Descendants(svc + "_WeighingNo").Single().Value, Is.EqualTo("A&B<C>"));
    }

    [Test]
    public void ParseReturnValue_ExtractsValueFromNavResponse()
    {
        // Response shape observed live from the NAV endpoint.
        string response =
            "<Soap:Envelope xmlns:Soap=\"http://schemas.xmlsoap.org/soap/envelope/\"><Soap:Body>" +
            "<WeighingFromMicroting2_Result xmlns=\"urn:microsoft-dynamics-schemas/codeunit/MicrotingWS\">" +
            "<return_value>Vejenr. V250790 opdateret med Yes</return_value>" +
            "</WeighingFromMicroting2_Result></Soap:Body></Soap:Envelope>";

        Assert.That(NavSoap.ParseReturnValue(response), Is.EqualTo("Vejenr. V250790 opdateret med Yes"));
    }

    [Test]
    public void ParseReturnValue_ReturnsNull_WhenAbsent()
    {
        string response =
            "<Soap:Envelope xmlns:Soap=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
            "<Soap:Body><SomethingElse /></Soap:Body></Soap:Envelope>";

        Assert.That(NavSoap.ParseReturnValue(response), Is.Null);
    }
}
