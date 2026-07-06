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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microting.eForm.Dto;
using Microting.eForm.Infrastructure;
using Microting.eForm.Infrastructure.Constants;
using Microting.eForm.Infrastructure.Data.Entities;
using Microting.eForm.Infrastructure.Models;
using Microting.eFormTrashInspectionBase.Infrastructure.Data;
using Microting.eFormTrashInspectionBase.Infrastructure.Data.Entities;
using Rebus.Handlers;
using ServiceTrashInspectionPlugin.Infrastructure.Helpers;
using ServiceTrashInspectionPlugin.Messages;
using TrashInspectionServiceReference;
using CheckListValue = Microting.eForm.Infrastructure.Models.CheckListValue;
using Field = Microting.eForm.Infrastructure.Models.Field;
using FieldValue = Microting.eForm.Infrastructure.Models.FieldValue;

namespace ServiceTrashInspectionPlugin.Handlers;

public class eFormCompletedHandler : IHandleMessages<eFormCompleted>
{
    private readonly eFormCore.Core _sdkCore;
    private readonly TrashInspectionPnDbContext _dbContext;

    public eFormCompletedHandler(eFormCore.Core sdkCore, DbContextHelper dbContextHelper)
    {
        _dbContext = dbContextHelper.GetDbContext();
        _sdkCore = sdkCore;
    }

#pragma warning disable 1998
    public async Task Handle(eFormCompleted message)
    {

        Console.WriteLine("[DBG] TrashInspection: We got a message : " + message.caseId);
        TrashInspectionCase trashInspectionCase =
            _dbContext.TrashInspectionCases.SingleOrDefault(x => x.SdkCaseId == message.caseId.ToString());
        if (trashInspectionCase != null)
        {

            #region get case information

            CaseDto caseDto = await _sdkCore.CaseLookupMUId(message.caseId);
            await using MicrotingDbContext microtingDbContext = _sdkCore.DbContextHelper.GetDbContext();
            var microtingUId = caseDto.MicrotingUId;
            var microtingCheckUId = caseDto.CheckUId;
            Language language = await microtingDbContext.Languages.SingleAsync(x => x.Name == "Danish");
            ReplyElement theCase = await _sdkCore.CaseRead((int)microtingUId, (int)microtingCheckUId, language);
            CheckListValue dataElement = (CheckListValue)theCase.ElementList[0];
            bool inspectionApproved = false;
            string approvedValue = "";
            string comment = "";
            Console.WriteLine("[DBG] Trying to find the field with the approval value");
            foreach (var field in dataElement.DataItemList)
            {
                Field f = (Field) field;
                if (f.Label.Contains("Angiv om læs er Godkendt"))
                {
                    Console.WriteLine($"The field is {f.Label}");
                    FieldValue fv = f.FieldValues[0];
                    String fieldValue = fv.Value;
                    inspectionApproved = (fieldValue == "1");
                    approvedValue = fieldValue;
                    Console.WriteLine($"[DBG] We are setting the approved state to {inspectionApproved.ToString()}");
                }

                if (f.Label.Equals("Kommentar"))
                {
                    Console.WriteLine($"[DBG] The field is {f.Label}");
                    FieldValue fv = f.FieldValues[0];
                    String fieldValue = fv.Value;
                    comment = fieldValue;
                    Console.WriteLine($"[DBG] We are setting the comment to {comment.ToString()}");
                }
            }
            #endregion

            Console.WriteLine("TrashInspection: The incoming case is a trash inspection related case");
            trashInspectionCase.Status = 100;
            await trashInspectionCase.Update(_dbContext);

            TrashInspection trashInspection =
                _dbContext.TrashInspections.SingleOrDefault(x => x.Id == trashInspectionCase.TrashInspectionId);
            if (trashInspection != null)
            {
                trashInspection.Status = 100;
                trashInspection.IsApproved = inspectionApproved;
                trashInspection.Comment = comment;
                trashInspection.ApprovedValue = approvedValue;
                trashInspection.InspectionDone = true;
                await trashInspection.Update(_dbContext);

                List<TrashInspectionCase> trashInspectionCases = _dbContext.TrashInspectionCases
                    .Where(x => x.TrashInspectionId == trashInspection.Id).ToList();
                foreach (TrashInspectionCase inspectionCase in trashInspectionCases)
                {
                    if (await _sdkCore.CaseDelete(int.Parse(inspectionCase.SdkCaseId)))
                    {
                        inspectionCase.WorkflowState = Constants.WorkflowStates.Retracted;
                        await inspectionCase.Update(_dbContext);
                    }
                }

                #region get settings

                string callBackUrl = _dbContext.PluginConfigurationValues
                    .SingleOrDefault(x => x.Name == "TrashInspectionBaseSettings:callBackUrl")?.Value;
                Console.WriteLine("[DBG] callBackUrl is : " + callBackUrl);

                string callBackCredentialDomain = _dbContext.PluginConfigurationValues.SingleOrDefault(x =>
                    x.Name == "TrashInspectionBaseSettings:CallBackCredentialDomain")?.Value;
                Console.WriteLine("[DBG] callBackCredentialDomain is : " + callBackCredentialDomain);

                string callbackCredentialUserName = _dbContext.PluginConfigurationValues.SingleOrDefault(x =>
                    x.Name == "TrashInspectionBaseSettings:callbackCredentialUserName")?.Value;
                Console.WriteLine("[DBG] callbackCredentialUserName is : " + callbackCredentialUserName);

                string callbackCredentialPassword = _dbContext.PluginConfigurationValues.SingleOrDefault(x =>
                    x.Name == "TrashInspectionBaseSettings:CallbackCredentialPassword")?.Value;
                Console.WriteLine("[DBG] callbackCredentialPassword is : " + callbackCredentialPassword);

                string callbackCredentialAuthType = _dbContext.PluginConfigurationValues.SingleOrDefault(x =>
                    x.Name == "TrashInspectionBaseSettings:CallbackCredentialAuthType")?.Value;
                Console.WriteLine("[DBG] callbackCredentialAuthType is : " + callbackCredentialAuthType);

                Console.WriteLine($"[DBG] trashInspection.WeighingNumber is {trashInspection.WeighingNumber}");
                #endregion

                switch (callbackCredentialAuthType)
                {
                    case "NTLM":
                        await CallUrlNtlmAuth(callBackUrl, callBackCredentialDomain, callbackCredentialUserName,
                            callbackCredentialPassword, trashInspection, inspectionApproved);
                        break;
                    case "basic":
                    default:
                        await CallUrlBaiscAuth(callBackUrl, callBackCredentialDomain, callbackCredentialUserName,
                            callbackCredentialPassword, trashInspection, inspectionApproved);
                        break;
                }
            }
        }

    }

    private async Task CallUrlBaiscAuth(string callBackUrl, string callBackCredentialDomain,
        string callbackCredentialUserName, string callbackCredentialPassword, TrashInspection trashInspection,
        bool inspectionApproved)
    {

        ChannelFactory<MicrotingWS_Port> factory;
        MicrotingWS_Port serviceProxy;
        BasicHttpBinding basicHttpBinding =
            new BasicHttpBinding();
        basicHttpBinding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
        basicHttpBinding.Security.Transport.ClientCredentialType =
            HttpClientCredentialType.Basic;
        factory =
            new ChannelFactory<MicrotingWS_Port>(basicHttpBinding,
                new EndpointAddress(
                    new Uri(callBackUrl)));

        if (callBackCredentialDomain != "...")
        {
            factory.Credentials.Windows.ClientCredential.Domain = callBackCredentialDomain;
        }

        factory.Credentials.UserName.UserName = callbackCredentialUserName;
        factory.Credentials.UserName.Password = callbackCredentialPassword;

        serviceProxy = factory.CreateChannel();
        ((ICommunicationObject)serviceProxy).Open();

        try
        {
            WeighingFromMicroting2 weighingFromMicroting2 =
                new WeighingFromMicroting2(trashInspection.WeighingNumber, inspectionApproved);
            Task<WeighingFromMicroting2_Result> result =
                serviceProxy.WeighingFromMicroting2Async(weighingFromMicroting2);


            Console.WriteLine("[DBG] Result is " + result.Result.return_value);
            trashInspection.SuccessMessageFromCallBack = result.Result.return_value;
            trashInspection.ResponseSendToCallBackUrl = true;
            await trashInspection.Update(_dbContext);

        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERR] We got the following error: " + ex.Message);
            trashInspection.ErrorFromCallBack = ex.Message;
            await trashInspection.Update(_dbContext);
        }
        finally
        {
            // cleanup
            factory.Close();
            ((ICommunicationObject)serviceProxy).Close();
            // *** ENSURE CLEANUP *** \\
            //CloseCommunicationObjects((ICommunicationObject)serviceProxy, factory);
            //OperationContext.Current = prevOpContext; // Or set to null if you didn't capture the previous context
        }
    }

    private async Task CallUrlNtlmAuth(string callBackUrl, string callBackCredentialDomain,
        string callbackCredentialUserName, string callbackCredentialPassword, TrashInspection trashInspection,
        bool inspectionApproved)
    {
        // WCF's ChannelFactory NTLM handshake fails on modern .NET against servers that offer only
        // 'WWW-Authenticate: NTLM' (single scheme), which is exactly what this NAV endpoint returns
        // (dotnet/wcf #4520, #4094, #5515), so we issue the SOAP call via HttpClient. NTLM on the
        // Linux container additionally requires gss-ntlmssp + the OpenSSL legacy provider, enabled in
        // Dockerfile-service, otherwise the handshake crypto is unavailable and the server returns 401.
        string soapBody = NavSoap.BuildWeighingFromMicroting2Envelope(trashInspection.WeighingNumber, inspectionApproved);

        NetworkCredential credential = callBackCredentialDomain != "..."
            ? new NetworkCredential(callbackCredentialUserName, callbackCredentialPassword, callBackCredentialDomain)
            : new NetworkCredential(callbackCredentialUserName, callbackCredentialPassword);

        using HttpClientHandler handler = new HttpClientHandler { Credentials = credential };
        using HttpClient httpClient = new HttpClient(handler);

        try
        {
            using StringContent content = new StringContent(soapBody, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", $"\"{NavSoap.WeighingFromMicroting2Action}\"");

            HttpResponseMessage response = await httpClient.PostAsync(callBackUrl, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string wwwAuthenticate = response.Headers.WwwAuthenticate.Count > 0
                    ? string.Join(", ", response.Headers.WwwAuthenticate)
                    : "(none)";
                string error = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}. " +
                               $"WWW-Authenticate: {wwwAuthenticate}. Body: {responseBody}";
                Console.WriteLine("[ERR][NTLM] Callback failed: " + error);
                trashInspection.ErrorFromCallBack = error;
                await trashInspection.Update(_dbContext);
                return;
            }

            string returnValue = NavSoap.ParseReturnValue(responseBody);

            Console.WriteLine("[DBG][NTLM] Result is " + returnValue);
            trashInspection.SuccessMessageFromCallBack = returnValue;
            trashInspection.ResponseSendToCallBackUrl = true;
            await trashInspection.Update(_dbContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERR][NTLM] Exception during callback: " + ex.Message);
            trashInspection.ErrorFromCallBack = ex.Message;
            await trashInspection.Update(_dbContext);
        }
    }
}