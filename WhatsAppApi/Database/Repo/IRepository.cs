using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Persister.Entity;

namespace WhatsAppApi.Database.Repo
{
    public abstract class IRepository<T>
    {
        private Type _ClassType;

        protected IRepository(Type aClass)
        {
            _ClassType = aClass;
        }

        public Boolean Delete(T server)
        {
            using (ISession session = HibernateHelper.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {
                    try
                    {
                        session.Delete(server);
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        return false;
                    }
                    finally
                    {
                        session.Disconnect();
                        session.Close();
                    }
                }
            }
        }

        public Boolean DeleteAll()
        {
            AbstractEntityPersister metadata = HibernateHelper.SessionFactory.GetClassMetadata(typeof(T)) as NHibernate.Persister.Entity.AbstractEntityPersister;
            String table = metadata.TableName;

            using (ISession session = HibernateHelper.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {
                    try
                    {
                        string deleteAll = String.Format("Delete From \"{0}\"", table);
                        session.CreateSQLQuery(deleteAll).ExecuteUpdate();
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        return false;
                    }
                    finally
                    {
                        session.Disconnect();
                        session.Close();
                    }
                }
            }
        }

        public T Get(int id)
        {
            using (ISession session = HibernateHelper.OpenSession())
            {
                return session.Get<T>(id);
            }
        }

        public IList<T> GetAll()
        {
            using (ISession session = HibernateHelper.OpenSession())
            {
                return session.CreateCriteria(_ClassType).List<T>();
            }
        }

        public Boolean Save(T server)
        {
            using (ISession session = HibernateHelper.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {
                    try
                    {
                        session.Save(server);
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        return false;
                    }
                    finally
                    {
                        session.Disconnect();
                        session.Close();
                    }
                }
            }
        }

        public IList<T> ExecuteSqlQuery(String sql)
        {
            List<T> retVal = new List<T>();

            using (ISession session = HibernateHelper.OpenSession())
            {
                try
                {
                    ISQLQuery sqlQuery = session.CreateSQLQuery(sql);
                    IList rResult = sqlQuery.List();
                    foreach (Object data in rResult)
                    {
                        retVal.Add((T)data);
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    session.Disconnect();
                    session.Close();
                }
            }

            return retVal;
        }

        public IList<T> ExecuteCriteria(Dictionary<String, Object> criteriaList)
        {
            List<T> retVal = new List<T>();

            using (ISession session = HibernateHelper.OpenSession())
            {
                try
                {
                    ICriteria result = session.CreateCriteria(_ClassType);
                    foreach (KeyValuePair<String, Object> criteria in criteriaList)
                    {
                        result.Add(Restrictions.Eq(criteria.Key, criteria.Value));
                    }
                    IList rResult = result.List();
                    foreach (Object data in rResult)
                    {
                        retVal.Add((T)data);
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    session.Disconnect();
                    session.Close();
                }
            }

            return retVal;
        }

        public Boolean SaveOrUpdate(T server)
        {
            using (ISession session = HibernateHelper.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {
                    try
                    {
                        session.SaveOrUpdate(server);
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        return false;
                    }
                    finally
                    {
                        session.Disconnect();
                        session.Close();
                    }
                }
            }
        }
        public Boolean Update(T server)
        {
            using (ISession session = HibernateHelper.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {
                    try
                    {
                        session.Update(server);
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        return false;
                    }
                    finally
                    {
                        session.Disconnect();
                        session.Close();
                    }
                }
            }
        }
    }
}
