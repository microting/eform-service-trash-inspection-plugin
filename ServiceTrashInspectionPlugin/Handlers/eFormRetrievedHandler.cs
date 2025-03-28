﻿/*
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
using System.Threading.Tasks;
using Microting.eFormTrashInspectionBase.Infrastructure.Data;
using Microting.eFormTrashInspectionBase.Infrastructure.Data.Entities;
using Rebus.Handlers;
using ServiceTrashInspectionPlugin.Infrastructure.Helpers;
using ServiceTrashInspectionPlugin.Messages;

namespace ServiceTrashInspectionPlugin.Handlers;

public class eFormRetrievedHandler : IHandleMessages<eFormRetrieved>
{
    private readonly eFormCore.Core _sdkCore;
    private readonly TrashInspectionPnDbContext _dbContext;

    public eFormRetrievedHandler(eFormCore.Core sdkCore, DbContextHelper dbContextHelper)
    {
        _dbContext = dbContextHelper.GetDbContext();
        _sdkCore = sdkCore;
    }

#pragma warning disable 1998
    public async Task Handle(eFormRetrieved message)
    {
        Console.WriteLine("TrashInspection: We got a message : " + message.caseId);
        TrashInspectionCase trashInspectionCase = _dbContext.TrashInspectionCases.SingleOrDefault(x => x.SdkCaseId == message.caseId.ToString());
        if (trashInspectionCase != null)
        {
            Console.WriteLine("TrashInspection: The incoming case is a trash inspection related case");
            if (trashInspectionCase.Status < 77)
            {
                trashInspectionCase.Status = 77;
                await trashInspectionCase.Update(_dbContext);
            }

            TrashInspection trashInspection = _dbContext.TrashInspections.SingleOrDefault(x => x.Id == trashInspectionCase.TrashInspectionId);
            if (trashInspection != null)
            {
                if (trashInspection.Status < 77)
                {
                    trashInspection.Status = 77;
                    await trashInspection.Update(_dbContext);
                }    
            }
        }
    }
}