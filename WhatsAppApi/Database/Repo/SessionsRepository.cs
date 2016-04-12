using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsAppApi.Database.Orm;

namespace WhatsAppApi.Database.Repo
{
    public class SessionsRepository : IRepository<Sessions>
    {
        public SessionsRepository() :
            base(typeof(Sessions))
        {
        }

        public List<Sessions> GetSessions(String recipientId,uint deviceId)
        {
            Dictionary<String, object> criteriaList = new Dictionary<string, object>();
            criteriaList.Add("RecipientId", recipientId);
            criteriaList.Add("DeviceId", deviceId);

            List<Sessions> preKeys = (List<Sessions>)ExecuteCriteria(criteriaList);
            return preKeys;
        }

        public List<Sessions> GetSessions(String recipientId)
        {
            Dictionary<String, object> criteriaList = new Dictionary<string, object>();
            criteriaList.Add("RecipientId", recipientId);

            List<Sessions> preKeys = (List<Sessions>)ExecuteCriteria(criteriaList);
            return preKeys;
        }

        public bool Contains(String recipientId, uint deviceId)
        {
            Dictionary<String, object> criteriaList = new Dictionary<string, object>();
            criteriaList.Add("RecipientId", recipientId);
            criteriaList.Add("DeviceId", deviceId);

            List<Sessions> preKeys = (List<Sessions>)ExecuteCriteria(criteriaList);
            return preKeys != null && preKeys.Count > 0;
        }
    }
}
