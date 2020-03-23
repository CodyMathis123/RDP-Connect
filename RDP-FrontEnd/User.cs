using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDP_FrontEnd
{
    public class User
    {
        public User(string domain, string accountName, string fullAccountName)
        {
            this.Domain = domain;
            this.AcountName = accountName;
            this.FullAccountName = fullAccountName;
        }

        public string Domain { get; set; }
        public string AcountName { get; set; }
        public string FullAccountName { get; set; }
    }
}