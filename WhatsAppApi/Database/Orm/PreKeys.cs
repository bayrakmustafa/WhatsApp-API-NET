using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace WhatsAppApi.Database.Orm
{
    public class PreKeys
    {
        public virtual int Id
        {
            get;
            set;
        }

        public virtual String PreKeyId
        {
            get;
            set;
        }

        public virtual bool SentToServer
        {
            get;
            set;
        }
        public virtual byte[] Record
        {
            get;
            set;
        }
    }
    public class PreKeysMap : ClassMapping<PreKeys>
    {
        public PreKeysMap()
        {
            Table("PreKeys");
            Id(x => x.Id, map => map.Generator(Generators.Identity));
            Property(x => x.PreKeyId, map => map.Unique(true));
            Property(x => x.SentToServer);
            Property(x => x.Record);
        }
    }
}
