using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyslogMover
{
    internal class SyslogInfo
    {
        public string Ip { get; set; }
        public string TimeStamp { get; set; }
        public string Hostname { get; set; }
        public string Facility { get; set; }
        public string Priority { get; set; }
        public string AccessType { get; set; }
        public string Message { get; set; }

        public SyslogInfo() { }

        public SyslogInfo(string line)
        {
            var strArray = line.Split('	');
            if (strArray.Length == 7)
            {
                Ip = strArray[0];
                TimeStamp = strArray[1];
                Hostname = strArray[2];
                Facility = strArray[3];
                Priority = strArray[4];
                AccessType = strArray[5];
                Message = strArray[6];
            }
            else Console.WriteLine("Invalid input");
        }
    }
}
