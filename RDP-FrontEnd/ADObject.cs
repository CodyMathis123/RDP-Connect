using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDP_FrontEnd
{
    public class ADObject
    {
        public ADObject(string name, string distingishedName, string description)
        {
            this.Name = name;
            this.DistingishedName = distingishedName;
            this.Description = description;
        }

        public string Name { get; set; }
        public string DistingishedName { get; set; }
        public string Description { get; set; }
    }
}
