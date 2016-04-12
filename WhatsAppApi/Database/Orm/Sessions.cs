using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace WhatsAppApi.Database.Orm
{
    public class Sessions
    {
        public virtual int Id
        {
            get;
            set;
        }

        public virtual String RecipientId
        {
            get;
            set;
        }

        public virtual uint DeviceId
        {
            get;
            set;
        }
        public virtual byte[] Record
        {
            get;
            set;
        }

        public virtual long Timestamp
        {
            get;
            set;
        }
    }
    public class SessionsMap : ClassMapping<Sessions>
    {
        public SessionsMap()
        {
            Table("Sessions");
            Id(x => x.Id, map => map.Generator(Generators.Identity));
            Property(x => x.RecipientId, map => map.Unique(true));
            Property(x => x.DeviceId);
            Property(x => x.Record);
            Property(x => x.Timestamp);
        }
    }
}
