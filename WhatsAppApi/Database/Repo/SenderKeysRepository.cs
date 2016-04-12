using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsAppApi.Database.Orm;

namespace WhatsAppApi.Database.Repo
{
    public class SenderKeysRepository : IRepository<SenderKeys>
    {
        public SenderKeysRepository() : 
            base(typeof(SenderKeys))
        {
        }

        public List<SenderKeys> GetSenderKeys(String senderKeyId)
        {
            Dictionary<String, object> criteriaList = new Dictionary<string, object>();
            criteriaList.Add("SenderKeyId", senderKeyId);

            List<SenderKeys> preKeys = (List<SenderKeys>)ExecuteCriteria(criteriaList);
            return preKeys;
        }

        public bool Contains(String senderKeyId)
        {
            Dictionary<String, object> criteriaList = new Dictionary<string, object>();
            criteriaList.Add("SenderKeyId", senderKeyId);

            List<SenderKeys> preKeys = (List<SenderKeys>)ExecuteCriteria(criteriaList);
            return preKeys != null && preKeys.Count > 0;
        }
    }
}
