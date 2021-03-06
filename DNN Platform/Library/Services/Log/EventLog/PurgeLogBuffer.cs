﻿// 
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// 
#region Usings

using System;

using DotNetNuke.Services.Scheduling;

#endregion

namespace DotNetNuke.Services.Log.EventLog
{
    public class PurgeLogBuffer : SchedulerClient
    {
        public PurgeLogBuffer(ScheduleHistoryItem objScheduleHistoryItem)
        {
            ScheduleHistoryItem = objScheduleHistoryItem;
        }

        public override void DoWork()
        {
            try
            {
				//notification that the event is progressing
                Progressing(); //OPTIONAL
                LoggingProvider.Instance().PurgeLogBuffer();
                ScheduleHistoryItem.Succeeded = true; //REQUIRED
                ScheduleHistoryItem.AddLogNote("Purged log entries successfully"); //OPTIONAL
            }
            catch (Exception exc) //REQUIRED
            {
                ScheduleHistoryItem.Succeeded = false; //REQUIRED
                ScheduleHistoryItem.AddLogNote("EXCEPTION: " + exc); //OPTIONAL
                Errored(ref exc); //REQUIRED
                //log the exception
                Exceptions.Exceptions.LogException(exc); //OPTIONAL
            }
        }
    }
}
