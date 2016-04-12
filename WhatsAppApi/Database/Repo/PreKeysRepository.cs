using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate;
using WhatsAppApi.Database.Orm;

namespace WhatsAppApi.Database.Repo
{
    public class PreKeysRepository : IRepository<PreKeys>
    {
        public PreKeysRepository() :
            base(typeof(PreKeys))
        {
        }

        public List<PreKeys> GetPreKeys(String preKeyId)
        {
            Dictionary<String, object> criteriaList = new Dictionary<string, object>();
            criteriaList.Add("PreKeyId", preKeyId);

            List<PreKeys> preKeys = (List<PreKeys>)ExecuteCriteria(criteriaList);
            return preKeys;
        }

        public bool Contains(String preKeyId)
        {
            Dictionary<String, object> criteriaList = new Dictionary<string, object>();
            criteriaList.Add("PreKeyId", preKeyId);

            List<PreKeys> preKeys = (List<PreKeys>)ExecuteCriteria(criteriaList);
            return preKeys != null && preKeys.Count > 0;
        }
    }
}
