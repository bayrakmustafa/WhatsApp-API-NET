using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace WhatsAppApi.Database.Orm
{
    public class SignedPreKeys
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
    public class SignedPreKeysMap : ClassMapping<SignedPreKeys>
    {
        public SignedPreKeysMap()
        {
            Table("SignedPreKeys");
            Id(x => x.Id, map => map.Generator(Generators.Identity));
            Property(x => x.PreKeyId, map => map.Unique(true));
            Property(x => x.Record);
            Property(x => x.Timestamp);
        }
    }
}
