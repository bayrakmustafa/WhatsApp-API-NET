using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsAppApi.Database.Orm;

namespace WhatsAppApi.Database.Repo
{
    public class SignedPreKeysRepository : IRepository<SignedPreKeys>
    {
        public SignedPreKeysRepository() : 
            base(typeof(SignedPreKeys))
        {
        }

        public List<SignedPreKeys> GetSignedPreKeys(String signedPreKeyId)
        {
            Dictionary<String, object> criteriaList = new Dictionary<string, object>();
            criteriaList.Add("PreKeyId", signedPreKeyId);

            List<SignedPreKeys> preKeys = (List<SignedPreKeys>)ExecuteCriteria(criteriaList);
            return preKeys;
        }

        public bool Contains(String signedPreKeyId)
        {
            Dictionary<String, object> criteriaList = new Dictionary<string, object>();
            criteriaList.Add("PreKeyId", signedPreKeyId);

            List<SignedPreKeys> preKeys = (List<SignedPreKeys>)ExecuteCriteria(criteriaList);
            return preKeys != null && preKeys.Count > 0;
        }
    }
}
