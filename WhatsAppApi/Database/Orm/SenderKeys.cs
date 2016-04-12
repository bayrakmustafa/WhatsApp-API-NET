using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace WhatsAppApi.Database.Orm
{
    public class SenderKeys
    {
        public virtual int Id
        {
            get;
            set;
        }

        public virtual String SenderKeyId
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
    public class SenderKeysMap : ClassMapping<SenderKeys>
    {
        public SenderKeysMap()
        {
            Table("SenderKeys");
            Id(x => x.Id, map => map.Generator(Generators.Identity));
            Property(x => x.SenderKeyId, map => map.Unique(true));
            Property(x => x.Record);
        }
    }
}
