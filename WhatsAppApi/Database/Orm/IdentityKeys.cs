using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace WhatsAppApi.Database.Orm
{
    public class IdentityKeys
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

        public virtual String RegistrationId
        {
            get;
            set;
        }
        public virtual byte[] PublicKey
        {
            get;
            set;
        }

        public virtual byte[] PrivateKey
        {
            get;
            set;
        }

        public virtual uint NextPreKeyId
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
    public class IdentityKeysMap : ClassMapping<IdentityKeys>
    {
        public IdentityKeysMap()
        {
            Table("IdentityKeys");
            Id(x => x.Id, map => map.Generator(Generators.Identity));
            Property(x => x.RecipientId, map => map.Unique(true));
            Property(x => x.PublicKey);
            Property(x => x.PrivateKey);
            Property(x => x.NextPreKeyId);
            Property(x => x.Timestamp);
        }
    }
}
