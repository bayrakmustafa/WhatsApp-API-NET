using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using NHibernate.Mapping;
using NHibernate.Tool.hbm2ddl;

namespace WhatsAppApi.Database
{
    public static class DatabaseUtil
    {

        public static void InitializeDatabase()
        {
            SchemaUpdate schemaUpdate = new SchemaUpdate(HibernateHelper.Configuration);
            schemaUpdate.Execute(false, true);
        }
    }
}
