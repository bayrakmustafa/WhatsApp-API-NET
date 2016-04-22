using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsAppApi.Response
{
    public class WaBroadcast
    {
        public string Id
        {
            get; set;
        }
        public string Name
        {
            get; set;
        }
        public List<string> Recipients
        {
            get; set;
        } = new List<string>();

        public WaBroadcast(string id, string name, List<string> recipients)
        {
            this.Id = id;
            this.Name = name;
            this.Recipients = recipients;
        }
    }
}
