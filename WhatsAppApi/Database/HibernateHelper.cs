using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NHibernate.Cfg;
using WhatsAppApi.Database.Orm;

namespace WhatsAppApi.Database
{
    public static class HibernateHelper
    {
        private static ISessionFactory _sessionFactory;
        private static Configuration _configuration;
        private static HbmMapping _mapping;

        public static ISession OpenSession()
        {
            return SessionFactory.OpenSession();
        }

        public static ISessionFactory SessionFactory
        {
            get
            {
                return _sessionFactory ?? (_sessionFactory = Configuration.BuildSessionFactory());
            }
        }

        public static Configuration Configuration
        {
            get
            {
                return _configuration ?? (_configuration = CreateConfiguration(@"Config/NHibernate.xml"));
            }
        }

        public static HbmMapping Mapping
        {
            get
            {
                return _mapping ?? (_mapping = CreateMapping());
            }
        }

        private static Configuration CreateConfiguration(String configFile)
        {
            Configuration configuration = new Configuration();
            configuration.Configure(configFile);
            configuration.AddDeserializedMapping(Mapping, null);
            return configuration;
        }

        private static HbmMapping CreateMapping()
        {
            ModelMapper mapper = new ModelMapper();
            mapper.AddMappings(new List<Type> { typeof(IdentityKeysMap) });
            mapper.AddMappings(new List<Type> { typeof(PreKeysMap) });
            mapper.AddMappings(new List<Type> { typeof(SenderKeysMap) });
            mapper.AddMappings(new List<Type> { typeof(SessionsMap) });
            mapper.AddMappings(new List<Type> { typeof(SignedPreKeysMap) });
            return mapper.CompileMappingForAllExplicitlyAddedEntities();
        }
    }
}
