using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S3FileSync
{
    class WinEventLogger
    {
        public void ELog(string message, int eventid)
        {
            using (System.Diagnostics.EventLog eventLog = new EventLog("Application"))
            {
                /*
                //EventLog.CreateEventSource("MySource", "MyNewLog");
                if (!EventLog.SourceExists("S3Syncer"))
                {
                    eventLog.Source = "S3Syncer";
                }
                */
                eventLog.Source = "S3Syncer";
                string eventMessage = string.Format("The file {0} has been updated", message);
                eventLog.WriteEntry(eventMessage, EventLogEntryType.Warning, eventid, 1);
            }
        }
    }
}
