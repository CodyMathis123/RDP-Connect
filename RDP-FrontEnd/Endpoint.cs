using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDP_FrontEnd
{
    public class Endpoint
    {
        public Endpoint(string hostname, string operatingSystem, string description, string lastLoggedOnUser, bool rdpgw_Allow)
        {
            this.Hostname = hostname;
            this.OperatingSystem = operatingSystem;
            this.Description = description;
            this.LastLoggedOnUser = lastLoggedOnUser;
            this.RDPGW_Allow = rdpgw_Allow;
        }

        public string Hostname { get; set; }
        public string OperatingSystem { get; set; }
        public string Description { get; set; }
        public string LastLoggedOnUser { get; set; }
        public bool RDPGW_Allow { get; set; }
    }
}