using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsAppApi.Database.Orm;

namespace WhatsAppApi.Database.Repo
{
    public class IdentityKeysRepository : IRepository<IdentityKeys>
    {
        public IdentityKeysRepository() : 
            base(typeof(IdentityKeys))
        {
        }

        public List<IdentityKeys> GetIdentityKeys(String recipientId)
        {
            Dictionary<String, object> criteriaList = new Dictionary<string, object>();
            criteriaList.Add("RecipientId", recipientId);

            List<IdentityKeys> preKeys = (List<IdentityKeys>)ExecuteCriteria(criteriaList);
            return preKeys;
        }

        public bool Contains(String recipientId)
        {
            Dictionary<String, object> criteriaList = new Dictionary<string, object>();
            criteriaList.Add("RecipientId", recipientId);

            List<IdentityKeys> preKeys = (List<IdentityKeys>)ExecuteCriteria(criteriaList);
            return preKeys != null && preKeys.Count > 0;
        }
    }
}
